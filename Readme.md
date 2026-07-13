## 项目介绍
本系统以电商平台为业务背景，采用前后端分离架构（C# ASP.NET Core 9 + EntityFrameworkCore 9;  Next.js 14 + Shadcn UI组件库 Oracle 21c）。系统涵盖顾客、商家、管理员、客服四大角色。使用docker compose进行部署

>请Fork后在本地完成对应功能后再提交到自己的Fork仓库,最后再提交Pull Request就可以了.

## 任务安排
### 后端
#### 7.30前


#### 7.15前
1. 新建一个该项目的分支backend01,将backend/src/Models中的空文件用backend/docs文件夹中的数据库设计文档来填充,要用到的是EntityFrameworkCore9的写法.
2. 新建一个该项目的分支backend02,研究Oracle数据库驱动连接问题,在program.cs中验证是否能够正常连接并唤醒,并学习一下docker compose中Oracle配置的编写,参考backend/docs中的example.compose.yml中的oracle-db部分,是否可用,待下一步完成测试部分和docker compose文件才知道.
3. 新建一个该项目的分支backend03,使用minimal api的格式实现api.md中的对应路由,所有需要数据库返回数据的地方留一个空函数返回好用的测试口号即可.

第1个和第3个任务如果不会需要可以去学习一下,或者参考群里发的书😄
### 前端
#### 7.30前

#### 7.15前
1. 新建一个分支frontend01,学习next.js的app router编写方式,并实现如frontend/docs中的api.js中展示的所有测试路由.
2. 新建一个分支frontend02,在初步掌握tailwind css后(需要先学会基本的css),学习使用shadcn中提供的组件库,实现搭建一个基本的前端页面(这需要学会jsx语法),如frontend/docs中的单页应用.webp所展示的效果
3. 新建一个分支frontend03,有两种选择,实现一个即可:
   1. 学习zustand库,学会如何用zustand进行路由守卫,并实现一个基本的路由跳转展示
   2. 学习vitest编写,拿三个api实现一个简单的测试demo

>一律使用pnpm安装所需的依赖,如果用`npm`或者`yarn`有可能会爆出依赖冲突问题.

## 项目运行(待补充)

### 本地开发

#### 前端

#### 后端

### 一键运行
