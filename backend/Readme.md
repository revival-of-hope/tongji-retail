**后端**
## 项目构建与运行

`cd src`后运行了`dotnet new webapi`初始化了src文件夹.

当前开发阶段建议只用docker运行该项目,启动docker后在`src`文件夹中运行以下命令构建镜像(第一次可能会比较久):
```bash
docker build -t backend .
```
- 将制作的镜像命名为backend.

然后再运行这个镜像,映射到本机的8080端口(https://localhost:8080)
```bash
docker run -p 8080:8080 backend
```
再访问这个页面应该就可以了.

之后的每次运行也只要运行该命令就可以启动项目并看到api的效果:
```bash
docker run -p 8080:8080 backend
```

