<!--
 * @Author: King
 * @Date: 2020-10-30 15:54:40
 * @LastEditTime: 2020-10-30 15:56:36
 * @FilePath: \document\python\centos8下安装.md
-->
## Step 1: Update the Environment
Once again, to maintain best practices, let’s go ahead and ensure our system packages are all up to date.
```
[root@centos8 ~]# dnf update -y
```

## Step 2: Install Python 3
We are now ready to install Python 3.
```
[root@centos8 ~]# dnf install python3 -y
```

## Step 3: Verify the Python 3 Install
We can verify the installation and version of Python 3 the same way we did with Python 2.
```
[root@centos8 ~]# python3 -V
Python 3.7.5rc1
```

## Step 4: Running Python 3
Next, we can enter into a Python 3 shell environment by running the following command.
```
[root@centos8 ~]# python3
Python 3.6.8 (default, Nov 21 2019, 19:31:34)
[GCC 8.3.1 20190507 (Red Hat 8.3.1-4)] on linux
Type "help", "copyright", "credits" or "license" for more information.
>>>
```