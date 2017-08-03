# module.yaml
Информация о модуле содержится в файле `module.yaml`.

Каждый модуль может состоять из одной либо нескольких конфигураций (например конфигурация клиента, конфигурация с серверной частью, и конфигурация включающая тесты).

Каждая конфигурация может включать в себя следующие секции:

- deps
- build
- install/artifacts

Конфигурации можно наследовать, что бы избежать дублирования информации о зависимостях.

В модуле должна присутствовать конфигурация `full-build`, которая должна по возможности включать в себя остальные. Она будет использоваться при выполнении команд `cm get` и `cm build`.

Необязательная конфигурация `default` является родителем для всех остальных.

Пример:

	# секция описания конфигурации client
	client: 
	  # список зависимостей конфигурации client
	  # зависимость указывается в следующем виде: <moduleName>[@branchName][/configName]
	  # kanso - ссылка на модуль kanso. 
	  # kanso/client - ссылка на kanso в конфигурации client. 
	  # kanso@develop/client - kanso из ветки develop в конфигурации client
	  deps: 
	    - core
	    - log4net
	    - nunit
	    - logging
	    - http
	    - http.rp
	    - topology
	  
	  # информация о построении данного модуля в данной конфигурации
	  build:
		#build solution Kontur.Drive.sln in Client configuration
	    target: Kontur.Drive.sln
	    configuration: Client	   

	  # Информация о том, что является результатом билда данной конфигурации
	  install:
	    - bin/Kontur.Drive.Client.dll
	    - bin/Kontur.Drive.ServiceModel.dll
	    - module logging                      # Для работы Kontur.Drive.Client.dll нужна ссылка на результаты билда модуля logging
	    - nuget Newtonsoft.Json/6.0.8         # Либо нужена ссылка на nuget пакет
	  
	# Конфигурация sdk "расширяет" конфигурацию client (> client) и является конфигурацией по умолчанию (*default) (используется при добавлении ссылки на данный модуль)
	sdk > client *default:
	  # Для избежания дублирования списков зависимостей, deps от client наследуются сюда и их не нужно повторно прописывать
	  deps:
	    - auth
	    - libfront
	    - zebra
	    - kanso
	    - upload-service
	    - access-control
	    - zebra-utils
	    - config
	    - zookeeper
	  
	  # Секция build НЕ наследуется
	  build:
	    target: Kontur.Drive.sln
	    configuration: Sdk
	  
	  # Секция install наследуется, т.е. необходимо дописать лишь недостающие бинари
	  install:
	    - Kontur.Drive.TestHost/bin/Release/Kontur.Drive.TestHost.exe
	    - Kontur.Drive.TestHost/bin/Release/ServiceStack.Interfaces.dll
	  
	  
	# Конфигурация full-build расширяет client и sdk
	full-build > client, sdk:
	  deps:
	    # Так указывается diff на deps (в конфигурации full-build используется kanso/full-build. Сначала "удаляем" kanso из зависимостей, потом добавляем kanso/full-build)
	    - -kanso
	    - kanso/full-build
	 
	  build:
	    target: Kontur.Drive.sln
	    configuration: Release


# build section

#### Для модуля который не надо строить:

	build:
	  target: None
	  configuration: None

#### Если нужно указать дополнительные параметры:
	
	build:                                             
	  target: Solution.sln                              # Указание цели построения
	  tool:                                        
	    name: msbuild                                   # Инструмент для сборки. msbuild - для MSBuild.exe на Windows и xbuild на *nix.
	    version: "14.0"                                 # Версия msbuild, по умолчанию - последняя
	                                                    # set VS150COMNTOOLS=D:\Program Files\Microsoft Visual Studio\2017\Professional\Common7\Tools\
	                                                    # Обязательно в кавычках
	  configuration: Release                            # Конфигурация солюшена
	  parameters: "/p:WarningLevel=0"                   # Здесь можно указать свои параметры. Параметры по умолчанию не используются. Кавычки в параметрах экранируются символом '\'.
	                                                    # Параметры по умолчанию для msbuild:
	                                                    # /t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0
	                                                    # /p:VisualStudioVersion=14.0 (если явно указана)


#### Если в модуле несколько солюшенов
	
	build:
	  - name: a.release                                # Нужно назвать каждую часть
	    target: a.sln
	    configuration: release
	  - name: b.debug
	    target: b.sln
	    configuration: debug

#### Build with custom script:

build.xproj:

	<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
	    <Target Name="Rebuild">
	        <Exec Command="ping google.com > 1.txt" />
	    </Target>
	</Project>

module.yaml:

	full-build:
	  deps:
	  build:
	    target: build.xproj
	    configuration: Release

# Git hooks
	
	default:                                        # Описываем в конфигурации default
	  hooks:
	    - myhooks/pre-push                          # Путь до хука в репозитории
	                                                # будет скопирован в <module_name>/.git/hooks
	    - pre-commit.cement                         # Встроенный в цемент pre-commit хук, который проверит, что файлы содержащие не ASCII символы имеют кодировку UTF-8 with BOM
	                                                # не может быть более 1 хука с одинаковым именем
	  
	full-build *default:                            # Остальные конфигурации модуля


Если в текущей ветке есть хук, то при переключении на другую ветку он не удалится.
Если вы хотите использовать ваш хук с pre-commit.cement, просто добавьте его вызов в свой хук:

	.git/hooks/pre-commit.cement
	if [ $? -ne 0 ]; then
	  exit 1
	fi

