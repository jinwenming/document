#!/bin/bash
 
echo "Start"
 
export IP_ADDR=$(ip addr show enp7s0f0 | grep -Po 'inet \K[\d.]+')
echo $IP_ADDR
 
sudo su - << FOE
# Stop firewall and selinux
sudo systemctl disable --now firewalld
sudo /usr/sbin/setenforce 0
sudo sed -i 's/SELINUX=enforcing/SELINUX=permissive/g' /etc/selinux/config
# Ignore Swap Error while installing kubernetes cluster with Swap
cat<<EOF > /etc/sysconfig/kubelet
KUBELET_EXTRA_ARGS=--fail-swap-on=false
EOF
# Install neccessary system tools
sudo yum install -y dnf-utils
# Open ipvs
cat <<EOF >/etc/sysconfig/modules/ipvs.modules
modprobe -- ip_vs
modprobe -- ip_vs_rr
modprobe -- ip_vs_wrr
modprobe -- ip_vs_sh
modprobe -- nf_conntrack_ipv4
EOF
 
sudo chmod 755 /etc/sysconfig/modules/ipvs.modules
sudo bash /etc/sysconfig/modules/ipvs.modules
sudo lsmod | grep -e ip_vs -e nf_conntrack_ipv4
sudo dnf install ipset ipvsadm -y
# Config iptables
echo "br_netfilter" > /etc/modules-load.d/br_netfilter.conf
cat<<EOF > /etc/sysctl.d/k8s.conf
net.bridge.bridge-nf-call-ip6tables = 1
net.bridge.bridge-nf-call-iptables = 1
net.ipv4.ip_forward = 1
EOF
 
sudo modprobe br_netfilter
sudo sysctl --system
# Add Docker Repo
sudo dnf config-manager --add-repo https://mirrors.aliyun.com/docker-ce/linux/centos/docker-ce.repo
# Install Docker-CE
sudo dnf makecache timer
sudo dnf -y install --nobest docker-ce
# Enable Docker
sudo systemctl enable --now docker
# Config Docker
if [ ! -d "/etc/docker" ]; then
  mkdir /etc/docker
fi
 
cat<<EOF > /etc/docker/daemon.json
{
   "exec-opts": ["native.cgroupdriver=systemd"],
   "log-driver": "json-file",
   "log-opts": {
     "max-size": "100m"
   },
   "storage-driver": "overlay2",
   "storage-opts": [
     "overlay2.override_kernel_check=true"
   ],
   "registry-mirrors": ["https://docker.mirrors.ustc.edu.cn"]
}
EOF
 
sudo systemctl daemon-reload
sudo systemctl restart docker
# Add Kubernetes Repo
cat <<EOF > /etc/yum.repos.d/kubernetes.repo
[kubernetes]
name=Kubernetes
baseurl=https://mirrors.aliyun.com/kubernetes/yum/repos/kubernetes-el7-x86_64/
enabled=1
gpgcheck=1
repo_gpgcheck=1
gpgkey=https://mirrors.aliyun.com/kubernetes/yum/doc/yum-key.gpg https://mirrors.aliyun.com/kubernetes/yum/doc/rpm-package-key.gpg
EOF
 
sudo dnf install -y kubeadm kubectl kubelet
sudo systemctl enable kubelet
# Create Kubernetes Cluster
kubeadm init --pod-network-cidr=10.244.0.0/16 --apiserver-advertise-address=$IP_ADDR --kubernetes-version=1.18.0 --ignore-preflight-errors=Swap --image-repository registry.aliyuncs.com/google_containers
 
FOE
 
sleep 10s
# Add User to docker group
sudo usermod -a -G docker $(id -nu)
# Create .kube folder
if [ -f $HOME/.kube/config ]; then
  rm -rf $HOME/.kube/config
fi
 
if [ ! -d $HOME/.kube ]; then
  mkdir $HOME/.kube
fi
# Copy Kubernetes config file
sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config
sudo chown $(id -u):$(id -g) $HOME/.kube/config
# Apply network plugin
result=1
while [ $result -ne 0 ]
do
	kubectl apply -f https://raw.githubusercontent.com/coreos/flannel/master/Documentation/kube-flannel.yml
	result=$?
	sleep 10s
done
#kubectl apply -f https://docs.projectcalico.org/v3.10/manifests/calico.yaml
# Taint master node
kubectl taint nodes --all node-role.kubernetes.io/master-
 
echo "Complete"
