**后端**
## 任务安排
### 7.15前
1. 新建一个该项目的分支backend01,将backend/src/Models中的空文件用backend/docs文件夹中的数据库设计文档来填充,要用到的是EntityFrameworkCore9的写法.
2. 新建一个该项目的分支backend02,研究Oracle数据库驱动连接问题,在program.cs中验证是否能够正常连接并唤醒,并学习一下docker compose中Oracle配置的编写,参考backend/docs中的example.compose.yml中的oracle-db部分,是否可用,待下一步完成测试部分和docker compose文件才知道.
3. 新建一个该项目的分支backend03,使用minimal api的格式实现api.md中的对应路由,所有需要数据库返回数据的地方留一个空函数返回好用的测试口号即可.

第1个和第3个任务如果不会需要可以去学习一下,或者参考群里发的书😄

## 项目介绍

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

