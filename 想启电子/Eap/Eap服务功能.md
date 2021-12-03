1、EapAsyncForLinux 此服务用于机台报警定时统计，数据表：mcaeventstatisticbyday
2、WebBusiness 此服务用于机台报警入库，数据表：mcasecvmst，mcasecvdetail，mcasectime
3、WebMainFrameForEap 此服务用于与前端数据对接。
4、WebRecordData 此服务用于产量统计功能，数据表：maccount（此表没有用，以前的），maccountmst, maccountdetail
5、WebScan 此服务用于扫码枪功能。
6、WebMainHsms 此服务用于与机台通信，2.1 数据表： 不写数据表。
7、WebStatus 此服务用于设备状态功能 数据表：macstatus
8、WebRms 数据表：有好多，需要慢慢分析，用来与消息队列通信。

数据表：
mtbacode 报警代码