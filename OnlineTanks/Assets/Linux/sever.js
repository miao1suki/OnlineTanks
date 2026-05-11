const express=require('express');
const {exec}=require('child_process');

const app=express();

app.use(express.json());
app.use(express.urlencoded({extended:true}));

let rooms=[];

let availablePorts=[];

for(let p=7777;p<=7799;p++)
{
    availablePorts.push(p);
}


// 启动Unity实例
function startUnityServer(port, maxPlayers)
{
    exec(`chmod +x /home/ubuntu/LinuxSeverBuild/test1.x86_64`, (err)=>
    {
        if(err)
        {
            console.error("chmod失败", err);
            return;
        }

        let cmd =
`cd /home/ubuntu/LinuxSeverBuild &&
nohup ./test1.x86_64 \
-batchmode -nographics \
-server \
-port ${port} \
-maxPlayers ${maxPlayers} \
> room_${port}.log 2>&1 &`;

        exec(cmd,(err)=>
        {
            if(err)
            {
                console.error(`启动失败 ${port}`, err);
                return;
            }

            console.log(`Unity进程已启动 Port=${port}`);
        });
    });
}



// 创建房间
app.post('/api/createRoom',(req,res)=>
{
    if(!availablePorts.length)
    {
        return res.json({
            success:false
        });
    }

    let port=availablePorts.shift();

    console.log(`分配端口 ${port}`);

    let maxPlayers = Number(req.body.maxPlayers || 4);

    startUnityServer(port, maxPlayers);

    let room={
    id:Date.now(),
    roomName:req.body.name||"默认房间名",
    address:"62.234.93.20",
    port:port,
    playerCount:0,
    maxPlayers:maxPlayers,
    lastHeartbeat:Date.now()
};

    rooms.push(room);

    console.log(
        `房间注册成功 ${room.roomName} -> ${port}`
    );

    res.json({
        success:true,
        room
    });

});



// 房间列表
app.get('/api/rooms',(req,res)=>
{
    console.log(
        `客户端请求房间列表 数量=${rooms.length}`
    );

    res.json({
        rooms:rooms
    });
});



// 心跳
app.post('/api/heartbeat',(req,res)=>
{
    let port = Number(req.body.port);
    let playerCount = Number(req.body.playerCount ?? 0);
    let maxPlayers = Number(req.body.maxPlayers ?? 0);

    let room = rooms.find(r => r.port === port);

    if(room)
    {
        room.lastHeartbeat = Date.now();
        room.playerCount = playerCount;

        // 让大厅以Dedicated Server上报为准
        if (maxPlayers > 0) room.maxPlayers = maxPlayers;
    }

    res.json({ success:true });
});



// 删除房间
app.post('/api/removeRoom',(req,res)=>
{
    let port=req.body.port;

    let i=rooms.findIndex(
        r=>r.port==port
    );

    if(i!=-1)
    {
        exec(
`pkill -f -- "-port ${port}"`,
()=>{
console.log(
`Unity进程已关闭 ${port}`
);
}
        );

        // 放回队头 优先复用
        availablePorts.unshift(port);

        console.log(
            `端口回收 ${port}`
        );

        rooms.splice(i,1);
    }

    res.json({
        success:true
    });
});



// 自动清理
setInterval(()=>{

let now=Date.now();

rooms=rooms.filter(room=>{

if(now-room.lastHeartbeat>30000)
{
console.log(
`超时房间清理 ${room.port}`
);

exec(
`pkill -f -- "-port ${room.port}"`
);

availablePorts.unshift(
room.port
);

return false;
}

return true;

});

},10000);



app.listen(3000,()=>{

console.log(
"大厅HTTP启动 3000"
);

});