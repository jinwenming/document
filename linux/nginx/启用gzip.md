<!--
 * @Author: King
 * @Date: 2020-10-12 14:08:19
 * @LastEditTime: 2020-10-12 14:10:06
 * @FilePath: \document\linux\nginx\启用gzip.md
-->
## 1、在配置文件中加入如下代码：
```
server {
    listen  80;
    server_name localhost;
    
    gzip on;
    gzip_buffers 32 4K;
    gzip_comp_level 6;
    gzip_min_length 100;
    gzip_types application/javascript text/css text/xml;
    gzip_disable "MSIE [1-6]\."; #配置禁用gzip条件，支持正则。此处表示ie6及以下不启用gzip（因为ie低版本不支持）
    gzip_vary on;
}

```

gzip配置的常用参数

gzip on|off; #是否开启gzip

gzip_buffers 32 4K| 16 8K #缓冲(压缩在内存中缓冲几块? 每块多大?)

gzip_comp_level [1-9] #推荐6 压缩级别(级别越高,压的越小,越浪费CPU计算资源)

gzip_disable #正则匹配UA 什么样的Uri不进行gzip

gzip_min_length 200 # 开始压缩的最小长度(再小就不要压缩了,意义不在)

gzip_http_version 1.0|1.1 # 开始压缩的http协议版本(可以不设置,目前几乎全是1.1协议)

gzip_proxied # 设置请求者代理服务器,该如何缓存内容

gzip_types text/plain application/xml # 对哪些类型的文件用压缩 如txt,xml,html ,css

gzip_vary on|off # 是否传输gzip压缩标志

注意：

图片/mp3这样的二进制文件,不必压缩

因为压缩率比较小, 比如100->80字节,而且压缩也是耗费CPU资源的.

比较小的文件不必压缩,