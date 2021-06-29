# Host '192.168.48.1' is blocked because of many connection errors; unblock with 'mysqladmin flush-hos

## 方法一:刷新记录报错host的文件
```
mysqladmin  -uroot -p  -h192.168.1.1 flush-hosts
或者进入mysql
mysql -uroot -p
mysql>flush hosts;
```

## 方法二：进入数据库将max_connections参数调高，也可以在my.cnf文件中修改不过需要重启MySQL。

```
这是是查询数据库当前设置的最大连接数
mysql> show variables like '%max_connections%';
+-----------------+-------+
| Variable_name   | Value |
+-----------------+-------+
| max_connections | 1000  |
+-----------------+-------+

设置最大连接数
set GLOBAL max_connections=2000;
set GLOBAL max_user_connections=1500;

mysql> show global variables like "max%connections";
+----------------------+-------+
| Variable_name        | Value |
+----------------------+-------+
| max_connections      | 2000  |
| max_user_connections | 1500  |
+----------------------+-------+
2 rows in set (0.08 sec)
```

## 方法三：修改配置文件
```
# 允许最大连接数
max_connections=5000
max_user_connections=4000
max_connect_errors=1000
```