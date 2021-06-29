# 基于已有docker 密钥创建secret
### 首先通过docker login 获取docker登录信息
```
sudo docker login --username=workhub registry.cn-hangzhou.aliyuncs.com
```

### 创建secret存储credentials(注意替换config.json文件路径)
```
kubectl create secret generic regcred \
    --from-file=.dockerconfigjson=/root/.docker/config.json \
    --type=kubernetes.io/dockerconfigjson
```

### 在部署中使用secret
```
apiVersion: v1
kind: Pod
metadata:
  name: private-reg
spec:
  containers:
  - name: private-reg-container
    image: <your-private-image>
  imagePullSecrets:
  - name: regcred
```

# 直接通过yaml配置文件创建secret
### 注意修改namespace, 其中dockerconfigjson内容是经过base64 编码过的
```
apiVersion: v1
data:
  .dockerconfigjson: ewoJImF1dGhzIjogewoJCSJjY3IuY2NzLnRlbmNlbnR5dW4uY29tIjogewoJCQkiYXV0aCI6ICJNVEF3TURBME1qSTRNekUwT25kdmNtdG9kV0l0TWpBeE5nPT0iCgkJfSwKCQkicmVnaXN0cnkuY24taGFuZ3pob3UuYWxpeXVuY3MuY29tIjogewoJCQkiYXV0aCI6ICJkMjl5YTJoMVlqcGtiMk5yWlhJdGQyOXlhMmgxWWkweU1ERTUiCgkJfQoJfSwKCSJIdHRwSGVhZGVycyI6IHsKCQkiVXNlci1BZ2VudCI6ICJEb2NrZXItQ2xpZW50LzE4LjA5LjIgKGxpbnV4KSIKCX0KfQ==
kind: Secret
metadata:
  name: regcred
  namespace: custom
type: kubernetes.io/dockerconfigjson
```