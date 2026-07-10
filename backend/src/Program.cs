using Oracle.ManagedDataAccess.Client; // 目的：引入 Oracle 官方驱动命名空间，让文件能够识别 OracleConnection 等类。

// 第一部分：Web 服务器容器的初始化与配置准备

// 知识点：WebApplication.CreateBuilder(args)
// 解释：创建一个 Web 应用程序的建造者对象。
// 目的：它是后端的总管，负责在服务启动前，把依赖注入、配置文件读取、日志系统等全部打包准备好。
var builder = WebApplication.CreateBuilder(args);

// 知识点：builder.Services.AddOpenApi()
// 解释：向服务容器中注册 OpenAPI 功能。
// 目的：告诉系统项目以后需要自动生成接口测试文档，让服务器提前在后台把生成文档的工具准备好。
builder.Services.AddOpenApi();

// 知识点：builder.Build()
// 解释：根据上面配置好的所有依赖和材料，正式实例化出 Web 应用程序对象。
// 目的：从这一行开始配置阶段结束，真正的网站服务器对象诞生了。
var app = builder.Build();

// 第二部分：配置 HTTP 请求管道（中间件过滤层）

// 知识点：app.Environment.IsDevelopment()
// 解释：检查当前的运行环境。如果在本地开发阶段跑代码，这里就会返回 true。
// 目的：安全保护。只有在本地开发阶段，才允许暴露接口测试文档，如果是以后发布到线上环境，这个判断为 false，文档自动隐藏。
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 知识点：app.UseHttpsRedirection()
// 解释：启动强制 HTTPS 重定向中间件。
// 目的：安全保护。如果用户误敲了不安全的 http 链接访问后端，这行代码会强制把他重定向到安全的 https 链路。
app.UseHttpsRedirection();

// 第三部分：核心任务2验证逻辑（必须夹在 app.Run 之前执行）

try
{
    //定义连接字符串。
    // 目的：存放数据库的拨号信息，包含 IP、端口、服务名、用户名和密码。
    string connString = "User Id=SYSTEM;Password=RetailSystem2024!;Data Source=localhost:1522/XE;";

    //在内存中实例化一个连接通道对象。
    // 应用：此时只是在本地创建了一个空壳对象，并没有产生实质的网络请求。
    OracleConnection conn = new OracleConnection(connString);

    try
    {
        // 核心 API 动作：真正开始网络拨号与握手。
        // 这行代码会跨越网络去连接远程数据库服务器。如果连接失败，代码会在此处抛出异常，并立刻跳出内层 try 块，进入外层 catch。
        conn.Open();

        // 逻辑：只有当 conn.Open() 成功连通、没有触发任何报错时，代码才能执行到这一行。
        Console.WriteLine("Oracle 数据库已被成功唤醒并连通！");
    }
    finally
    {
        // 无论上面 conn.Open() 是顺利通关，还是抛出异常，只要离开内层 try 块，就必须强制执行 finally 内部的代码。
        if (conn != null)
        {
            // 彻底释放、关闭网络通道。
            // 防止因为网络故障将死连接持续挂在服务器上，确保释放服务器端口。
            conn.Dispose();
        }
    }
}
catch (Exception ex)
{
    //如果网络连接失败，内层的异常会砸到这里。程序不会崩溃，而是在控制台打印出让人一目了然的错误原因。
    Console.WriteLine("数据库连接失败，原因为：");
    Console.WriteLine(ex.Message);
}

// 第四部分：服务正式起跑

// 注释掉原先拦截全盘的欢迎页
// app.UseWelcomePage();

// app.Run()
//让服务器开始不间断地监听网络端口。
//这是一个死循环。一旦执行这一行，程序就会在这里保持运行，等待前端给它发请求。因为它是死循环，所以任务2的验证代码必须写在它的上方。
app.Run();