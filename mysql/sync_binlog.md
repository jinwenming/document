sync_binlog ：这个参数是对于MySQL系统来说是至关重要的，他不仅影响到Binlog对MySQL所带来的性能损耗，而且还影响到MySQL中数据的完整性。对于“sync_binlog”参数的各种设置的说明如下：

1、sync_binlog=0，当事务提交之后，MySQL不做fsync之类的磁盘同步指令刷新binlog_cache中的信息到磁盘，而让Filesystem自行决定什么时候来做同步，或者cache满了之后才同步到磁盘。

2、sync_binlog=n，当每进行n次事务提交之后，MySQL将进行一次fsync之类的磁盘同步指令来将binlog_cache中的数据强制写入磁盘。

在MySQL中系统默认的设置是sync_binlog=0，也就是不做任何强制性的磁盘刷新指令，这时候的性能是最好的，但是风险也是最大的。因为一旦系统Crash，在binlog_cache中的所有binlog信息都会被丢失。而当设置为“1”的时候，是最安全但是性能损耗最大的设置。因为当设置为1的时候，即使系统Crash，也最多丢失binlog_cache中未完成的一个事务，对实际数据没有任何实质性影响，就是对写入性能影响太大，binlog虽然是顺序IO，多个事务同时提交，同样很大的影响MySQL和IO性能。虽然可以通过group commit的补丁缓解，但是刷新的频率过高对IO的影响也非常大。对于高并发事务的系统说，“sync_binlog”设置为0和设置为1的系统写入性能差距可能高达5倍甚至更多。

所以很多MySQL DBA设置的sync_binlog并不是最安全的1，而是100、1000 或者是0。这样牺牲一定的一致性，可以获得更高的并发和吞吐量！

通过我的实验，将sync_binlog设置为0。发现根本就不写入binlog！也许是我没有等到filesystem自行同步。。。

高负载下， 主库sync_binlog=1 可能造成主库的binlog 不能及时推送到从库（反应在从库的show slave status\G上就是 second_behind_master一会儿为0 一会儿又变的很大）

```
SET GLOBAL sync_binlog=0
```