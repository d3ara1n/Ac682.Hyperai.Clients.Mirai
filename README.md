# Ac682.Hyperai.Clients.Mirai

使用 [mirai-api-http](https://github.com/project-mirai/mirai-api-http) 来建立与 [mirai](https://github.com/mamoe/mirai) 的通讯并提供消息接收发送能力.

底层使用 [Hyperai](https://github.com/theGravityLab/Hyperai/).

## 使用 | Usage

添加 nuget 包
```bash
dotnet add package Ac682.Hyperai.Clients.Mirai
```

#### 例子 | Examples

```csharp
var options = new MiraiClientOptions()
{
    AuthKey = "IDK",
    Port = 1234,
    Host = "localhost",
    SelfQQ = 10000
};
var client = new MiraiClient(options);

client.Connect();

var handler = new DefaultEventHandler(Reply);
client.On<FriendMesasgeEventArgs>(handler);
client.Listen();

void Reply(FriendMessageEventArgs args)
{
    var @event = new FriendMessageEventArgs()
    {
        User = args.User,
        Message = MessageChain.Construct(new Plain("[自动回复]有事不在, 稍后回复."))
    };
    client.SendAsync(@event).Wait();
}
```

## 用在 HyperaiShell 中 | Support for HyperaiShell

直接把 [.nupkg 文件](https://www.nuget.org/packages/Ac682.Hyperai.Clients.Mirai/)塞到 HyperaiShell 的 `./plugins` 目录就行.

HyperaiShell 配置文件 `appsettings.json` 中关于客户端的内容修改如下
```json
{    
    "Application": 
    {
        "SelectedClientName": "Mirai"
    },
    "Clients": [
        {
            "Name": "Mirai",
            "ClientTypeDefined": "Ac682.Hyperai.Clients.Mirai.MiraiClient,Ac682.Hyperai.Clients.Mirai",
            "OptionsTypeDefined": "Ac682.Hyperai.Clients.Mirai.MiraiClientOptions,Ac682.Hyperai.Clients.Mirai",
            "Options": {
                "Port": YOUR_MIRAI_API_HTTP_PORT,
                "Host": "YOUR_MIRAI_API_HTTP_HOST",
                "AuthKey": "YOUR_MIRAI_API_HTTP_AUTHKEY",
                "SelfQQ": YOUR_BOT_QQ
            }
        }
    ]
}
```