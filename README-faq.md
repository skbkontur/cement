# Frequently asked questions

#### 0. I can't build or clone some module, is it cement problem?
Cement just finds your local `git` and `msbuild`, and executes commands.

Your can always run this comands in console, without cement.

#### 1. How to use msbuild installed in custom location?
Fill VS150COMNTOOLS variable with your custom location:

`set VS150COMNTOOLS=D:\Program Files\Microsoft Visual Studio\2017\Professional\Common7\Tools\`
