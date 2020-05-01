# Windows Update Remote Service
> Control the Windows Update service on multiple servers with a single minimalistic GUI client.

![The client interface of the Windows Update Remote Service][head-img]

- [Windows Update Remote Service](#windows-update-remote-service)
	- [Why](#why)
	- [Key concepts](#key-concepts)
		- [Windows Update session](#windows-update-session)
		- [State machine](#state-machine)
		- [Communication, authentication and autorisation](#communication-authentication-and-autorisation)
	- [Requirements](#requirements)
	- [Installation](#installation)
		- [Agent](#agent)
			- [Configure App.config](#configure-appconfig)
			- [Configure Log.config](#configure-logconfig)
			- [Register service](#register-service)
			- [Least privileges](#least-privileges)
		- [Client](#client)
	- [Getting started](#getting-started)
	- [Development setup](#development-setup)
	- [Release history](#release-history)
	- [Meta](#meta)
	- [Project status and Contributing](#project-status-and-contributing)

## Why

Years ago, I had to manage a lot of Windows servers. There were patch evenings on a regular basis. In spite of the availability of [WSUS] and group policies, it was complicated to automate the patch process for a multitude of reasons. For example:

* Some servers had to run shaky services, which depended on services on other hosts. When their dependencies were not available on startup or became unavailable later in the patch window, they crashed. Thus, a strict patch sequence needed to be followed. 
* A small license USB dongle prevented a bare metal Windows host from booting, so I had to coordinate the reboot of this specific host with the site manager. 
* Some servers needed manual work from coworkers, before I was allowed to execute specific patch steps like installing updates or rebooting. 

...and so on.

I was tired of the many manual RDP logins to coordinate the dependencies between the "manual intervention" servers.

There were only expensive "Windows Update" management applications on the market, with a lot of features I was not interested in. So I wrote my own small solution.

## Key concepts

The project consists of two parts, an agent that needs to be installed on the Windows servers you want to manage, and a [WPF] client application.

The client can connect to agents and send commands like "search", "select update", "download", "install" and "reboot". The agent will push progress and state changes back to all connected clients.

The agent uses the [Windows Update Agent API] provided by Microsoft to transmit the commands from the clients to Windows Update. **The agent itself does not do any search, download or installation of updates. This is all handled by the Windows Update service**. The advantage is, that there is no custom update routine which can damage the system. That also means that configurations for WSUS will be respected.

Regardless of changes in Microsoft's update policy with newer OS versions, the [Windows Update Agent API] seems to have been stable for the last couple of years.

### Windows Update session

When the agent starts, it creates an [update session] to communicate with Windows Update. Operations in one update session are isolated from operations in other update sessions. For example, the Windows Update menu of the control panel uses it's own update session. Thereby, the activity of the agent is not shown in the Windows Update menu of the control panel. Nevertheless, Windows Update handles concurrent sessions correctly. Only one session at time is able to install updates. The agent reacts to such situations and reports that the requested operation is not possible due activity of other sessions. Updates, that are downloaded by other sessions, will not be downloaded again, all sessions use the same "update storage".

### State machine

The [Windows Update Agent API] protects the agent from doing invalid actions, but also needs to be interfaced correctly. The agent manages an internal state machine to implement a valid patch flow. The client can use commands to advance through the states. Invalid transitions will be rejected. Some transitions occur automatically based on events in the Windows Update session.

![Activity diagram that shows valid state transitions.][statemachine-img]

Some transitions have preconditions, which are not shown in the diagram. E.g. to enter the "Downloading" state, there must be updates available, which are not already downloaded. The client knows these rules and disables inappropriate commands for the user.

### Communication, authentication and autorisation

The solution is designed to be used in a classic Windows Active Directory environment.

The communication between client and agent is realized with [WCF] over the tcp protocol. By default the communication is [encrypted and signed] at [transportation level] by using TLS.

To authenticate connections between the client and the agents, Kerberos or NTLM authentication will be performed, depending on the host configuration. The client will be authenticated with the Windows user context, in which it is running. The agent rejects any user that does not have local administrator privileges on its host. This means, to manage a group of servers, you need to run the client, using an Active Directory user that has these permissions on all managed hosts. This authorisation behavior is currently hardcoded.

When UAC is turned on, you may need to start the client with elevated permissions to gain the administrator privileges.

## Requirements

> The solution is designed to be used in a classic Windows Active Directory environment.

The agent is designed as long running process with a small footprint on the host system. While the agent itself does not consume a lot of CPU cycles, remember that Windows Update can and will use a considerable amount of ressources, when an action is requested.

* Agent/Client: Windows Server 2008 R2 / Windows 7 up to Windows Server 2016 / Windows 10 and .NET Framework 4.5.2
* Agent: Differences between desktop and core variants could not be observed
* Agent: 10-20 MBytes of disk and RAM space

## Installation

### Agent

Unpack the zip and copy the content to the desired location. Before you start the agent or install it as service, you may want to configure the agent. The agent is operable with the default settings. For testing purposes, you can start the agent as console application by simply executing ``WcfWuRemoteService.exe``.

#### Configure App.config

There are only a few settings, that makes sense to be changed:

| XML Attribute      | Description                                                                                                                                                               |
|--------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| autoAcceptEulas    | Some updates requires EULA acceptance. When enabled, the EULAs will be accepted automatically before download or installation. Manual acceptance is possible via the client.           |
| autoSelectUpdates  | When enabled, important updates will automatically be selected for installation. You are still able to add/remove the selection of updates with the client.                                                                                        |
| createFirewallRule | When enabled, the agent tries to register itself in the windows firewall and opens the configured port for its binary. On shutdown, the agent tries to remove this rule. |
| [baseAddress]      | [Configures the binding]. You may want change the port or binding IP.   |                                    
```xml
<configuration>
	<wuapicontroller autoSelectUpdates="true" autoAcceptEulas="false" />
	<wuservice createFirewallRule="false" />
	<system.serviceModel>
		<services>
			<service name="WcfWuRemoteService.WuRemoteService">
				<host>
					<baseAddresses>
						<add baseAddress="net.tcp://0.0.0.0:8523/WuRemoteService" />
					</baseAddresses>
				</host>
			</service>
		</services>
	</system.serviceModel>
</configuration>
```

#### Configure Log.config

To configure the log, visit [Apache log4net™ Manual - Configuration]. You should adjust the log file path and log level to your needs.

```xml
<log4net>
  <appender name="RollingFileAppender" type="log4net.Appender.RollingFileAppender">
    <file value="log.txt" />
  </appender>
  <root>
    <level value="Info" />
  </root>
</log4net>
```

#### Register service

Here is a PowerShell example to register the agent as Windows service. The service will be executed with ``LocalSystem`` privileges.

```PowerShell
PS> New-Service -Name 'WuRemoteService' -DisplayName "Windows Update Remote Service" -BinaryPathName '<Path to extracted WcfWuRemoteService.exe>' -StartupType Automatic
PS> Start-Service 'WuRemoteService'
```

To uninstall, execute ``sc.exe delete WuRemoteService`` and remove the files.

#### Least privileges

The agent was only tested with ``LocalSystem`` privileges. I never tried out the least privilege requirements. If you want to try, these are my suggestions to start with:

* Set ``createFirewallRule`` to ``false``. This avoids the usage of COM interfaces ``FwCplLua`` and ``NetFwTypeLib`` to configure the Windows firewall.
* To run a [WCF] application as non administrator, you must [configure http.sys] to grant binding permissions for the least privileged service user.
* The least privileged service user must be able to use the COM interface ``WUApiLib`` to interact with Windows Update. Maybe this can be done with [DCOMCNFG].
* The least privileged service user may need the "[logon as a service]" right.

Please send me your "least privileges solution" if you come up with one. I will update this section with your results.

### Client

Unpack the zip and copy the content to the desired location, then execute ``WcfWuRemoteClient.exe``. When you get "access denied" messages while connecting to agents, despite local administrator privileges on the target hosts, start the client with elevated privileges. 

## Getting started

1. For your first steps on your local machine, just unpack the zip and start ``WcfWuRemoteClient.exe`` and ``WcfWuRemoteService.exe``. You need local administrator privileges and you may need to start the proccess with right click and "Run as administrator" for full elevated privileges.
2. In the client application, click on "Add Host", the "Add Host" window appears. The pre-inserted URL ``net.tcp://localhost:8523/WuRemoteService`` should be fine, so just click on "Add".
3. Select the connected host from the "Hostlist" and click "Search Updates". The agent is now requested to search for Windows updates.
4. When the search completes, double click on the host in the hostlist to get details about the updates that have been found.

You should now see something like that: ![The client shows three found updates. In the background the agent runs in console mode.][usage-example-img]

Depending on the search result, you can now take other actions.

## Development setup

I upgraded the origin solution built with Visual Studio 2015 to be compatible with Visual Studio 2019. I also migrated all contained projects to the new SDK format. You need to install "Windows Communication Foundation" and ".NET desktop development workload" with Visual Studio Installer. The solution uses MS Test as unit test framework.

If you compile with DEBUG configuration, the agent uses ``WuApiMocks.WuApiSimulator``, which mimics basic behavior of ``WindowsUpdateApiController.WuApiController``. No changes to your OS are made, when you install the simulated updates. This allows independent development of the [WCF] (communication) and [WPF] (client) stuff. The DEBUG configuration also disables the "local administrator" autorisation check. So there is no need to run Visual Studio with local administrator privileges, while debugging the applications.

Developing the ``WindowsUpdateApiController.WuApiController`` can be very challenging, when you need some integration tests with the [Windows Update Agent API]. How to tell windows to fail an installation to see how ``WuApiController`` reacts?

For some integration tests, I used a VM to quickly reset to a state without patches. Limiting the disk space on the system drive while Windows Update expands/installs updates, is one way to provoke a failure. With [WSUS] you can better control, which updates are presented to Windows Update.

## Release history

This is the first public release.

In 2015, I investigated the ``WUApiLib`` and wrote a prototype. The biggest part of the solution was written in 2016 as a personal coding project. I added some small enhancements in 2017 to fit my use cases better. In 2018, I removed the GUI-installer/MSI part from the solution, because the native support for such project types was removed from Visual Studio. For the public release in 2020, I added license informations. I also ensured that Visual Studio 2019 is able to build the project and upgraded to the new SDK format.

## Meta

Developed by Elia Seikritt – git@seikritt.ch,
[https://github.com/EliaSaSe](https://github.com/EliaSaSe)

The preferred method of communication is via GitHub.

Distributed under the GNU Lesser General Public License. See ``COPYING`` and ``COPYING.LESSER`` for more information.

This project uses the following libraries:

* [log4net](https://logging.apache.org/log4net/), Copyright 2004-2017 The Apache Software Foundation, [Apache License, Version 2.0 License](http://www.apache.org/licenses/LICENSE-2.0)
* [moq](https://github.com/moq/moq), Copyright (c) 2017 Daniel Cazzulino and Contributors, [MIT License](https://github.com/moq/moq/blob/master/LICENSE)
* [Feather](https://feathericons.com), Copyright (c) 2013-2017 Cole Bemis, [MIT License](https://github.com/feathericons/feather/blob/master/LICENSE)
* [WCF -- Windows Communication Foundation Client Libraries](https://github.com/dotnet/wcf), Copyright (c) .NET Foundation and Contributors, [MIT Licence](https://github.com/dotnet/wcf/blob/master/LICENSE.TXT)
* [Microsoft Test Framework "MSTest V2"](https://github.com/microsoft/testfx), Copyright (c) Microsoft Corporation. All rights reserved, [MIT Licence](https://github.com/microsoft/testfx/blob/master/LICENSE.txt)
* [CCSWE.Core](https://github.com/CoryCharlton/CCSWE.Core), Copyright 2015 Cory Charlton, [Apache License, Version 2.0 License](http://www.apache.org/licenses/LICENSE-2.0)

## Project status and Contributing

The project is not under active development, but I'm still using the software for some edge cases. I try to keep the project compatible with current Visual Studio versions to be able to compile binaries as long as I'm using this software.

Currently there is no contribution guideline. If you are interested in contributing, raise an issue to let me know. I will then add such a guideline.

<!-- Markdown link & img dfn's -->
[Apache log4net™ Manual - Configuration]: https://logging.apache.org/log4net/release/manual/configuration.html
[baseAddress]: https://docs.microsoft.com/dotnet/framework/configure-apps/file-schema/wcf/baseaddresses
[Configures the binding]: https://docs.microsoft.com/dotnet/framework/wcf/specifying-an-endpoint-address
[WCF]: https://docs.microsoft.com/dotnet/framework/wcf/whats-wcf
[WPF]: https://docs.microsoft.com/dotnet/framework/wpf/getting-started/introduction-to-wpf-in-vs
[encrypted and signed]: https://docs.microsoft.com/dotnet/framework/wcf/understanding-protection-level
[transportation level]: https://docs.microsoft.com/dotnet/framework/wcf/feature-details/transport-security-overview
[update session]: https://docs.microsoft.com/windows/win32/api/wuapi/nn-wuapi-iupdatesession3
[Windows Update Agent API]: https://docs.microsoft.com/windows/win32/wua_sdk/portal-client
[Apache log4net™ Manual - Configuration]: https://logging.apache.org/log4net/release/manual/configuration.html
[WSUS]: https://docs.microsoft.com/windows-server/administration/windows-server-update-services/get-started/windows-server-update-services-wsus
[configure http.sys]: https://docs.microsoft.com/dotnet/framework/wcf/feature-details/configuring-http-and-https
[DCOMCNFG]: https://docs.microsoft.com/cpp/atl/dcomcnfg?view=vs-2019
[logon as a service]: https://docs.microsoft.com/previous-versions/windows/it-pro/windows-server-2012-R2-and-2012/dn221981(v=ws.11)
[statemachine-img]: ./Images/states.png
[usage-example-img]: ./Images/usage_example.png
[head-img]: ./Images/head.png
