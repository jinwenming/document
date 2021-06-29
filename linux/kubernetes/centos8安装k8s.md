## 1、系统准备
查看系统版本
```
[root@localhost]# cat /etc/centos-release
CentOS Linux release 8.1.1911 (Core)
```
配置主机名
```
[root@localhost ~]# hostnamectl set-hostname k8s-master01
```
关闭防火墙
```
[root@k8s-master01 ~]# systemctl stop firewalld
[root@k8s-master01 ~]# systemctl disable firewalld
[root@k8s-master01 ~]# setenforce 0
```
配置网络
```
[root@k8s-master01 ~]# cat /etc/sysconfig/network-scripts/ifcfg-enp0s3
TYPE=Ethernet
PROXY_METHOD=none
BROWSER_ONLY=no
BOOTPROTO=static
DEFROUTE=yes
IPV4_FAILURE_FATAL=no
IPV6INIT=yes
IPV6_AUTOCONF=yes
IPV6_DEFROUTE=yes
IPV6_FAILURE_FATAL=no
IPV6_ADDR_GEN_MODE=stable-privacy
NAME=enp0s3
UUID=039303a5-c70d-4973-8c91-97eaa071c23d
DEVICE=enp0s3
ONBOOT=yes
IPADDR=192.168.122.21
NETMASK=255.255.255.0
GATEWAY=192.168.122.1
DNS1=223.5.5.5
```

添加阿里源
```
[root@k8s-master01 ~]# rm -rfv /etc/yum.repos.d/*
[root@k8s-master01 ~]# curl -o /etc/yum.repos.d/CentOS-Base.repo http://mirrors.aliyun.com/repo/Centos-8.repo
```

关闭swap，注释swap分区
```
[root@k8s-master01 ~]# swapoff -a
[root@k8s-master01 ~]# cat /etc/fstab

#
# /etc/fstab
# Created by anaconda on Tue Mar 31 22:44:34 2020
#
# Accessible filesystems, by reference, are maintained under '/dev/disk/'.
# See man pages fstab(5), findfs(8), mount(8) and/or blkid(8) for more info.
#
# After editing this file, run 'systemctl daemon-reload' to update systemd
# units generated from this file.
#
/dev/mapper/cl-root     /                       xfs     defaults        0 0
UUID=5fecb240-379b-4331-ba04-f41338e81a6e /boot                   ext4    defaults        1 2
/dev/mapper/cl-home     /home                   xfs     defaults        0 0
#/dev/mapper/cl-swap     swap                    swap    defaults        0 0
```
配置内核参数，将桥接的IPv4流量传递到iptables的链
```
[root@k8s-master01 ~]# cat > /etc/sysctl.d/k8s.conf <<EOF
net.bridge.bridge-nf-call-ip6tables = 1
net.bridge.bridge-nf-call-iptables = 1
EOF
sysctl --system
```

## 2、安装常用包
```
[root@k8s-master01 ~]# yum install vim bash-completion net-tools gcc -y
```

## 3、使用aliyun源安装docker-ce
```
[root@k8s-master01 ~]# yum install -y yum-utils device-mapper-persistent-data lvm2
[root@k8s-master01 ~]# yum-config-manager --add-repo https://mirrors.aliyun.com/docker-ce/linux/centos/docker-ce.repo
[root@k8s-master01 ~]# yum -y install docker-ce
```
安装docker-ce如果出现以下错
```
[root@k8s-master01 ~]# yum -y install docker-ce
CentOS-8 - Base - mirrors.aliyun.com                                                                               14 kB/s | 3.8 kB     00:00
CentOS-8 - Extras - mirrors.aliyun.com                                                                            6.4 kB/s | 1.5 kB     00:00
CentOS-8 - AppStream - mirrors.aliyun.com                                                                          16 kB/s | 4.3 kB     00:00
Docker CE Stable - x86_64                                                                                          40 kB/s |  22 kB     00:00
Error:
 Problem: package docker-ce-3:19.03.8-3.el7.x86_64 requires containerd.io >= 1.2.2-3, but none of the providers can be installed
  - cannot install the best candidate for the job
  - package containerd.io-1.2.10-3.2.el7.x86_64 is excluded
  - package containerd.io-1.2.13-3.1.el7.x86_64 is excluded
  - package containerd.io-1.2.2-3.3.el7.x86_64 is excluded
  - package containerd.io-1.2.2-3.el7.x86_64 is excluded
  - package containerd.io-1.2.4-3.1.el7.x86_64 is excluded
  - package containerd.io-1.2.5-3.1.el7.x86_64 is excluded
  - package containerd.io-1.2.6-3.3.el7.x86_64 is excluded
(try to add '--skip-broken' to skip uninstallable packages or '--nobest' to use not only best candidate packages)
```
解决方法
```
[root@k8s-master01 ~]# wget https://download.docker.com/linux/centos/7/x86_64/edge/Packages/containerd.io-1.2.6-3.3.el7.x86_64.rpm
[root@k8s-master01 ~]# yum install containerd.io-1.2.6-3.3.el7.x86_64.rpm
```
然后再安装docker-ce即可成功

添加aliyundocker仓库加速器
```
[root@k8s-master01 ~]# mkdir -p /etc/docker
[root@k8s-master01 ~]# tee /etc/docker/daemon.json <<-'EOF'
{
  "registry-mirrors": ["https://fl791z1h.mirror.aliyuncs.com"]
}
EOF
[root@k8s-master01 ~]# systemctl enable docker
[root@k8s-master01 ~]# systemctl daemon-reload
[root@k8s-master01 ~]# systemctl restart docker
```

## 4、安装kubectl、kubelet、kubeadm
添加阿里kubernetes源
```
[root@k8s-master01 ~]# cat <<EOF > /etc/yum.repos.d/kubernetes.repo
[kubernetes]
name=Kubernetes
baseurl=https://mirrors.aliyun.com/kubernetes/yum/repos/kubernetes-el7-x86_64/
enabled=1
gpgcheck=1
repo_gpgcheck=1
gpgkey=https://mirrors.aliyun.com/kubernetes/yum/doc/yum-key.gpg https://mirrors.aliyun.com/kubernetes/yum/doc/rpm-package-key.gpg
EOF
```
安装
```
[root@k8s-master01 ~]# yum install kubectl kubelet kubeadm
[root@k8s-master01 ~]# systemctl enable kubelet
```

## 5、初始化k8s集群
```
cat > /etc/sysconfig/modules/ipvs.modules << EOF
#!/bin/bash
modprobe -- ip_vs
modprobe -- ip_vs_rr
modprobe -- ip_vs_wrr
modprobe -- ip_vs_sh
modprobe -- nf_conntrack_ipv4
EOF

chmod 755 /etc/sysconfig/modules/ipvs.modules && bash /etc/sysconfig/modules/ipvs.modules && lsmod | grep -e ip_vs -track_ipv4
```
方法一
```
[root@k8s-master01 ~]# kubeadm config print init-defaults > kubeadm-config.yaml
[root@k8s-master01 ~]# vim kubeadm-config.yaml
[root@k8s-master01 ~]# kubeadm init --config=kubeadm-config.yaml --experimental-upload-certs | tee kubeadm-init.log
```
方法二
```
[root@k8s-master01 ~]# kubeadm init --kubernetes-version=1.18.0  \
--apiserver-advertise-address=36.154.57.51   \
--image-repository registry.aliyuncs.com/google_containers  \
--service-cidr=10.96.0.0/12 --pod-network-cidr=10.244.0.0/16
```
POD的网段为: 10.244.0.0/16， api server地址就是master本机IP。
这一步很关键，由于kubeadm 默认从官网k8s.grc.io下载所需镜像，国内无法访问，因此需要通过–image-repository指定阿里云镜像仓库地址。
集群初始化成功后返回如下信息：
```
W0408 09:36:36.121603   14098 configset.go:202] WARNING: kubeadm cannot validate component configs for API groups [kubelet.config.k8s.io kubeproxy.config.k8s.io]
[init] Using Kubernetes version: v1.18.0
[preflight] Running pre-flight checks
        [WARNING FileExisting-tc]: tc not found in system path
[preflight] Pulling images required for setting up a Kubernetes cluster
[preflight] This might take a minute or two, depending on the speed of your internet connection
[preflight] You can also perform this action in beforehand using 'kubeadm config images pull'
[kubelet-start] Writing kubelet environment file with flags to file "/var/lib/kubelet/kubeadm-flags.env"
[kubelet-start] Writing kubelet configuration to file "/var/lib/kubelet/config.yaml"
[kubelet-start] Starting the kubelet
[certs] Using certificateDir folder "/etc/kubernetes/pki"
[certs] Generating "ca" certificate and key
[certs] Generating "apiserver" certificate and key
[certs] apiserver serving cert is signed for DNS names [master01.paas.com kubernetes kubernetes.default kubernetes.default.svc kubernetes.default.svc.cluster.local] and IPs [10.10.0.1 192.168.122.21]
[certs] Generating "apiserver-kubelet-client" certificate and key
[certs] Generating "front-proxy-ca" certificate and key
[certs] Generating "front-proxy-client" certificate and key
[certs] Generating "etcd/ca" certificate and key
[certs] Generating "etcd/server" certificate and key
[certs] etcd/server serving cert is signed for DNS names [master01.paas.com localhost] and IPs [192.168.122.21 127.0.0.1 ::1]
[certs] Generating "etcd/peer" certificate and key
[certs] etcd/peer serving cert is signed for DNS names [master01.paas.com localhost] and IPs [192.168.122.21 127.0.0.1 ::1]
[certs] Generating "etcd/healthcheck-client" certificate and key
[certs] Generating "apiserver-etcd-client" certificate and key
[certs] Generating "sa" key and public key
[kubeconfig] Using kubeconfig folder "/etc/kubernetes"
[kubeconfig] Writing "admin.conf" kubeconfig file
[kubeconfig] Writing "kubelet.conf" kubeconfig file
[kubeconfig] Writing "controller-manager.conf" kubeconfig file
[kubeconfig] Writing "scheduler.conf" kubeconfig file
[control-plane] Using manifest folder "/etc/kubernetes/manifests"
[control-plane] Creating static Pod manifest for "kube-apiserver"
[control-plane] Creating static Pod manifest for "kube-controller-manager"
W0408 09:36:43.343191   14098 manifests.go:225] the default kube-apiserver authorization-mode is "Node,RBAC"; using "Node,RBAC"
[control-plane] Creating static Pod manifest for "kube-scheduler"
W0408 09:36:43.344303   14098 manifests.go:225] the default kube-apiserver authorization-mode is "Node,RBAC"; using "Node,RBAC"
[etcd] Creating static Pod manifest for local etcd in "/etc/kubernetes/manifests"
[wait-control-plane] Waiting for the kubelet to boot up the control plane as static Pods from directory "/etc/kubernetes/manifests". This can take up to 4m0s
[apiclient] All control plane components are healthy after 23.002541 seconds
[upload-config] Storing the configuration used in ConfigMap "kubeadm-config" in the "kube-system" Namespace
[kubelet] Creating a ConfigMap "kubelet-config-1.18" in namespace kube-system with the configuration for the kubelets in the cluster
[upload-certs] Skipping phase. Please see --upload-certs
[mark-control-plane] Marking the node master01.paas.com as control-plane by adding the label "node-role.kubernetes.io/master=''"
[mark-control-plane] Marking the node master01.paas.com as control-plane by adding the taints [node-role.kubernetes.io/master:NoSchedule]
[bootstrap-token] Using token: v2r5a4.veazy2xhzetpktfz
[bootstrap-token] Configuring bootstrap tokens, cluster-info ConfigMap, RBAC Roles
[bootstrap-token] configured RBAC rules to allow Node Bootstrap tokens to get nodes
[bootstrap-token] configured RBAC rules to allow Node Bootstrap tokens to post CSRs in order for nodes to get long term certificate credentials
[bootstrap-token] configured RBAC rules to allow the csrapprover controller automatically approve CSRs from a Node Bootstrap Token
[bootstrap-token] configured RBAC rules to allow certificate rotation for all node client certificates in the cluster
[bootstrap-token] Creating the "cluster-info" ConfigMap in the "kube-public" namespace
[kubelet-finalize] Updating "/etc/kubernetes/kubelet.conf" to point to a rotatable kubelet client certificate and key
[addons] Applied essential addon: CoreDNS
[addons] Applied essential addon: kube-proxy

Your Kubernetes control-plane has initialized successfully!

To start using your cluster, you need to run the following as a regular user:

  mkdir -p $HOME/.kube
  sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config
  sudo chown $(id -u):$(id -g) $HOME/.kube/config

You should now deploy a pod network to the cluster.
Run "kubectl apply -f [podnetwork].yaml" with one of the options listed at:
  https://kubernetes.io/docs/concepts/cluster-administration/addons/

Then you can join any number of worker nodes by running the following on each as root:

kubeadm join 192.168.122.21:6443 --token v2r5a4.veazy2xhzetpktfz \
    --discovery-token-ca-cert-hash sha256:daded8514c8350f7c238204979039ff9884d5b595ca950ba8bbce80724fd65d4
[root@k8s-master01 ~]#
```
记录生成的最后部分内容，此内容需要在其它节点加入Kubernetes集群时执行。
根据提示创建kubectl
```
[root@k8s-master01 ~]#  mkdir -p $HOME/.kube
[root@k8s-master01 ~]# sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config
[root@k8s-master01 ~]#   sudo chown $(id -u):$(id -g) $HOME/.kube/config
```
执行下面命令，使kubectl可以自动补充
```
[root@k8s-master01 ~]# source <(kubectl completion bash)
```
查看节点，pod
```
[root@k8s-master01 ~]# kubectl get node
NAME                STATUS     ROLES    AGE     VERSION
master01.paas.com   NotReady   master   2m29s   v1.18.0
[root@k8s-master01 ~]# kubectl get pod --all-namespaces
NAMESPACE     NAME                                        READY   STATUS    RESTARTS   AGE
kube-system   coredns-7ff77c879f-fsj9l                    0/1     Pending   0          2m12s
kube-system   coredns-7ff77c879f-q5ll2                    0/1     Pending   0          2m12s
kube-system   etcd-master01.paas.com                      1/1     Running   0          2m22s
kube-system   kube-apiserver-master01.paas.com            1/1     Running   0          2m22s
kube-system   kube-controller-manager-master01.paas.com   1/1     Running   0          2m22s
kube-system   kube-proxy-th472                            1/1     Running   0          2m12s
kube-system   kube-scheduler-master01.paas.com            1/1     Running   0          2m22s
[root@k8s-master01 ~]#
```
node节点为NotReady，因为corednspod没有启动，缺少网络pod

## 6、安装calico网络
```
[root@k8s-master01 ~]# kubectl apply -f https://docs.projectcalico.org/manifests/calico.yaml
configmap/calico-config created
customresourcedefinition.apiextensions.k8s.io/bgpconfigurations.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/bgppeers.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/blockaffinities.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/clusterinformations.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/felixconfigurations.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/globalnetworkpolicies.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/globalnetworksets.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/hostendpoints.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/ipamblocks.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/ipamconfigs.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/ipamhandles.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/ippools.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/networkpolicies.crd.projectcalico.org created
customresourcedefinition.apiextensions.k8s.io/networksets.crd.projectcalico.org created
clusterrole.rbac.authorization.k8s.io/calico-kube-controllers created
clusterrolebinding.rbac.authorization.k8s.io/calico-kube-controllers created
clusterrole.rbac.authorization.k8s.io/calico-node created
clusterrolebinding.rbac.authorization.k8s.io/calico-node created
daemonset.apps/calico-node created
serviceaccount/calico-node created
deployment.apps/calico-kube-controllers created
serviceaccount/calico-kube-controllers created
```
查看pod和node
```
[root@k8s-master01 ~]# kubectl get pod --all-namespaces
NAMESPACE     NAME                                        READY   STATUS    RESTARTS   AGE
kube-system   calico-kube-controllers-555fc8cc5c-k8rbk    1/1     Running   0          36s
kube-system   calico-node-5km27                           1/1     Running   0          36s
kube-system   coredns-7ff77c879f-fsj9l                    1/1     Running   0          5m22s
kube-system   coredns-7ff77c879f-q5ll2                    1/1     Running   0          5m22s
kube-system   etcd-master01.paas.com                      1/1     Running   0          5m32s
kube-system   kube-apiserver-master01.paas.com            1/1     Running   0          5m32s
kube-system   kube-controller-manager-master01.paas.com   1/1     Running   0          5m32s
kube-system   kube-proxy-th472                            1/1     Running   0          5m22s
kube-system   kube-scheduler-master01.paas.com            1/1     Running   0          5m32s
[root@k8s-master01 ~]# kubectl get node
NAME                STATUS   ROLES    AGE     VERSION
master01.paas.com   Ready    master   5m47s   v1.18.0
[root@k8s-master01 ~]#
```

## 7、安装kubernetes-dashboard
下载recommended.yaml文件:
```
wget https://raw.githubusercontent.com/kubernetes/dashboard/v2.0.0/aio/deploy/recommended.yaml
```
如果出现
```
正在连接 raw.githubusercontent.com (raw.githubusercontent.com)|0.0.0.0|:443… 失败：拒绝连接。
正在连接 raw.githubusercontent.com (raw.githubusercontent.com)|::|:443… 失败：拒绝连接。
```
解决方法:(登录网站：https://www.ipaddress.com/。查询出raw.githubusercontent.com的IP地址:199.232.28.133)修改hosts文件
```
[root@k8s-master01 ~]# sudo vi /etc/hosts
```
在最后面添加199.232.28.133  raw.githubusercontent.com

修改recommended.yaml文件
```
[root@k8s-master01 ~]# vim recommended.yaml
```
修改的内容如下
```
---
kind: Service
apiVersion: v1
metadata:
  labels:
    k8s-app: kubernetes-dashboard
  name: kubernetes-dashboard
  namespace: kubernetes-dashboard
spec:
  type: NodePort #增加
  ports:
    - port: 443
      targetPort: 8443
      nodePort: 30000 #增加
  selector:
    k8s-app: kubernetes-dashboard
---
#因为自动生成的证书很多浏览器无法使用，所以我们自己创建，注释掉kubernetes-dashboard-certs对象声明
#apiVersion: v1
#kind: Secret
#metadata:
#  labels:
#    k8s-app: kubernetes-dashboard
#  name: kubernetes-dashboard-certs
#  namespace: kubernetes-dashboard
#type: Opaque
---
```
创建证书
```
[root@k8s-master01 ~]# mkdir dashboard-certs
[root@k8s-master01 ~]# cd dashboard-certs/

[root@k8s-master01 dashboard-certs]# kubectl create namespace kubernetes-dashboard

namespace/kubernetes-dashboard created

#创建命名空间
[root@k8s-master01 dashboard-certs]# openssl genrsa -out dashboard.key 2048
Generating RSA private key, 2048 bit long modulus (2 primes)
...........+++++
......................................+++++
e is 65537 (0x010001)

#证书请求
[root@k8s-master01 dashboard-certs]# openssl req -days 36000 -new -out dashboard.csr -key dashboard.key -subj '/CN=dashboard-cert'

Ignoring -days; not generating a certificate

#自签证书
[root@k8s-master01 dashboard-certs]# openssl x509 -req -in dashboard.csr -signkey dashboard.key -out dashboard.crt

Signature ok
subject=CN = dashboard-cert
Getting Private key

#创建kubernetes-dashboard-certs对象
[root@k8s-master01 dashboard-certs]# kubectl create secret generic kubernetes-dashboard-certs --from-file=dashboard.key --from-file=dashboard.crt -n kubernetes-dashboard

secret/kubernetes-dashboard-certs created
```

安装dashboard
```
[root@k8s-master01 ~]# kubectl create -f recommended.yaml
```
注意：这里可能会报如下所示(这是因为我们在创建证书时，已经创建了kubernetes-dashboard命名空间，所以，直接忽略此错误信息即可)
```
Error from server (AlreadyExists): error when creating "./recommended.yaml": namespaces "kubernetes-dashboard" already exists
```
查看pod，service
```
[root@k8s-master01 ~]# kubectl get pod --all-namespaces
NAME                                        READY   STATUS    RESTARTS   AGE
dashboard-metrics-scraper-dc6947fbf-869kf   1/1     Running   0          37s
kubernetes-dashboard-5d4dc8b976-sdxxt       1/1     Running   0          37s
[root@k8s-master01 ~]# kubectl get svc -n kubernetes-dashboard
NAME                        TYPE        CLUSTER-IP     EXTERNAL-IP   PORT(S)         AGE
dashboard-metrics-scraper   ClusterIP   10.10.58.93    <none>        8000/TCP        44s
kubernetes-dashboard        NodePort    10.10.132.66   <none>        443:30000/TCP   44s
[root@k8s-master01 ~]#
```
创建dashboard管理员
创建dashboard-admin.yaml文件。
```
[root@k8s-master01 ~]# vim dashboard-admin.yaml
```
文件的内容如下所示。
```
apiVersion: v1
kind: ServiceAccount
metadata:
  labels:
    k8s-app: kubernetes-dashboard
  name: dashboard-admin
  namespace: kubernetes-dashboard
```
保存退出后执行如下命令创建管理员。
```
[root@k8s-master01 ~]# kubectl create -f ./dashboard-admin.yaml
```
为用户分配权限
创建dashboard-admin-bind-cluster-role.yaml文件。
```
[root@k8s-master01 ~]# vim dashboard-admin-bind-cluster-role.yaml
```
文件内容如下所示。
```
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: dashboard-admin-bind-cluster-role
  labels:
    k8s-app: kubernetes-dashboard
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: cluster-admin
subjects:
- kind: ServiceAccount
  name: dashboard-admin
  namespace: kubernetes-dashboard
```
保存退出后执行如下命令为用户分配权限。
```
[root@k8s-master01 ~]# kubectl create -f ./dashboard-admin-bind-cluster-role.yaml
```

通过页面访问:https://192.168.122.21:30000

使用token进行登录，执行下面命令获取token
```
[root@k8s-master01 ~]# kubectl -n kubernetes-dashboard describe secret $(kubectl -n kubernetes-dashboard get secret | grep dashboard-admin | awk '{print $1}')
```

## 8、worker 节点加入
在master上执行下面命令获取加入的命令
```
[root@k8s-master01 ~]# kubeadm token create --print-join-command
W0526 10:51:36.917725   27326 configset.go:202] WARNING: kubeadm cannot validate component configs for API groups [kubelet.config.k8s.io kubeproxy.config.k8s.io]
kubeadm join 192.168.122.21:6443 --token 1oaspw.prawd4fx789uetjp     --discovery-token-ca-cert-hash sha256:ca037f5cb2bdb84baf555a5e76e54e1bff68a82720a878e1f4bc55e9a000c4cd
```
初始化 worker节点,在worker节点执行命令
```
[root@k8s-node01 ~]# echo "36.154.57.51  gojiaju.net" >> /etc/hosts
[root@k8s-node01 ~]# kubeadm join 192.168.122.21:6443 --token 1oaspw.prawd4fx789uetjp     --discovery-token-ca-cert-hash sha256:ca037f5cb2bdb84baf555a5e76e54e1bff68a82720a878e1f4bc55e9a000c4cd
```

## 9、检查初始化结果
在master上执行下面命令获取加入的命令
```
[root@k8s-master01 ~]# kubectl get nodes
```

## 10、移除 worker 节点
正常情况下，您无需移除 worker 节点，如果添加到集群出错，您可以移除 worker 节点，再重新尝试添加
在准备移除的 worker 节点上执行
只在 worker 节点执行
```
[root@k8s-node01 ~]# kubeadm reset
```
只在 master 节点执行
```
[root@k8s-master01 ~]# kubectl delete node demo-worker-x-x
```
将 demo-worker-x-x 替换为要移除的 worker 节点的名字
worker 节点的名字可以通过在节点 上执行 kubectl get nodes 命令获得


## 11、ingress-nginx
地址：https://github.com/kubernetes/ingress-nginx/tree/nginx-0.30.0/deploy
```
[root@master01 k8s]# kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/nginx-0.30.0/deploy/static/mandatory.yaml
[root@master01 k8s]# kubectl get pods -n ingress-nginx
```
配置教程：https://kubernetes.io/docs/concepts/services-networking/ingress/