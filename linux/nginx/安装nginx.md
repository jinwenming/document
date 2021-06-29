# Centos7 上安装nginx

## 1、安装编译工具及库文件
```
yum -y install make zlib zlib-devel gcc-c++ libtool  openssl openssl-devel
```
### 2、安装PCRE（让 Nginx 支持 Rewrite 功能）
```
cd /usr/local/src
#下载最新版本的，注意不要用pcre2
wget https://jaist.dl.sourceforge.net/project/pcre/pcre/8.42/pcre-8.42.tar.gz
tar -xvf pcre-8.42.tar.gz
cd pcre-8.42 
#安装编译
./configure && make && make install && pcre-config --version

```

## 3、 安装nginx
```
#下载
wget https://nginx.org/download/nginx-1.19.1.tar.gz
tar -xvf nginx-1.19.1.tar.gz
cd nginx-1.19.1
#编译安装
./configure --prefix=/usr/local/webserver/nginx --with-http_stub_status_module --with-http_ssl_module --with-pcre=/usr/local/src/pcre-8.42 &&make && make install
#查看版本
/usr/local/webserver/nginx/sbin/nginx -v
```

## 4、nginx 启动 停止 等命令
```
cd /usr/local/nginx/sbin/
./nginx
./nginx -s stop
./nginx -s quit
./nginx -s reload
```

## 5、 开机自启动
```
~]# vim usr/lib/systemd/system/nginx.service

[Unit]  
Description=nginx  
After=network.target  
   
[Service]  
Type=forking  
ExecStart=/usr/local/nginx/sbin/nginx 
ExecReload=/usr/local/nginx/sbin/nginx -s reload  
ExecStop=/usr/local/nginx/sbin/nginx -s stop
PrivateTmp=true  
   
[Install]  
WantedBy=multi-user.target

```
保存

设置开机自启
```
~]# systemctl enable nginx.service
```