<!--
 * @Author: King
 * @Date: 2020-09-07 16:43:14
 * @LastEditTime: 2020-10-13 09:54:19
 * @FilePath: \document\linux\frpc.md
-->
<!--
 * @Author: King
 * @Date: 2020-09-07 16:43:14
 * @LastEditTime: 2020-10-13 09:52:40
 * @FilePath: \document\linux\frpc.md
-->
## 服务端frpc 下载安装配置
```
~]# cd /usr/local/
~]# mkdir frp
~]# cd frp
~]# wget https://github.com/fatedier/frp/releases
~]# tar -zxvf frp_0.33.0_linux_amd64.tar.gz
~]# cd frp_0.33.0_linux_amd64
~]# rm -f frpc frpc.ini
~]# vi ./frps.ini
[common]
bind_port = 7000
vhost_http_port = 80
vhost_https_port = 443
```

# 保存，然后启动命令
```
~]# sudo vim /lib/systemd/system/frps.service
[Unit]
Description=fraps service
After=network.target syslog.target
Wants=network.target

[Service]
Type=simple
#启动服务的命令（此处写你的frps的实际安装目录）
ExecStart=/usr/local/frp/frp_0.33.0_linux_amd64/frps -c /usr/local/frp/frp_0.33.0_linux_amd64/frps.ini

[Install]
WantedBy=multi-user.target

~]# sudo systemctl start frps
~]# sudo systemctl enable frps
```

## 客户端frpc 下载安装配置
```
~]# cd /usr/local/
~]# mkdir frp
~]# cd frp
~]# wget https://github.com/fatedier/frp/releases
~]# tar -zxvf frp_0.34.1_linux_amd64.tar.gz
~]# cd frp_0.34.1_linux_amd64
~]# rm -f frps frps.ini
~]# vi ./frpc.ini
[common]
server_addr = 106.13.221.99
server_port = 7000

[web]
type = http
local_port = 80
local_ip = 127.0.0.1
custom_domains = *.gojiaju.net

```

# 保存，然后启动命令
```
~]# sudo vim /lib/systemd/system/frpc.service
[Unit]
Description=fraps service
After=network.target syslog.target
Wants=network.target

[Service]
Type=simple
#启动服务的命令（此处写你的frps的实际安装目录）
ExecStart=/usr/local/frp/frp_0.34.1_linux_amd64/frpc -c /usr/local/frp/frp_0.34.1_linux_amd64/frpc.ini

[Install]
WantedBy=multi-user.target

~]# sudo systemctl start frpc
~]# sudo systemctl enable frpc
```