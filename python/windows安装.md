# 下载python:
https://www.python.org/downloads/windows/

如果打不开换个浏览器试试

点击exe文件安装，安装完后把python与pip的路径添加到环境变量中。（pip在Scripts目录中）
```
C:\Python
C:\Python\Scripts
```
# pip 换国内源
在C:\Users\jinwe\目录下新建文件夹pip，并在文件夹下新建文件pip.ini 内容如下：
```
[global]
index-url=https://pypi.tuna.tsinghua.edu.cn/simple
[install]
trusted-host=https://pypi.tuna.tsinghua.edu.cn
```

# 安装selenium
```
pip install selenium
```

# 安装chrome浏览器驱动
下载Chromedriver的地址链接为：http://chromedriver.storage.googleapis.com/index.html

查询当前chrome浏览器的版本，找到对应的版本下载。下载完成后解压。将chromedriver.exe放到python的Scripts安装目录下。