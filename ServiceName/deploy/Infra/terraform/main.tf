# terraform init -backend-config="access_key="
# terraform apply -var-file="dev.tefvars"
provider "azurerm"  {
  # whilst the `version` attribute is optional, we recommend pinning to a given version of the Provider
  version = "1.33.1"
  subscription_id = "cf979063-433f-4e43-9a61-df40231fcd7e"
  client_id       = "${var.client_id}"
  client_secret   = "${var.client_secret}" 
  tenant_id       = "1bf562e0-3df8-4dd4-867b-11e99fa72ad4"
}

// provider "azurerm" {
//   alias  = "azure-accenture"
//   version = "1.33.1"
//   subscription_id = "cf979063-433f-4e43-9a61-df40231fcd7e"
//   client_id       = "${var.client_id}"
//   client_secret   = "${var.client_secret}" 
//   tenant_id       = "1bf562e0-3df8-4dd4-867b-11e99fa72ad4"
// }

terraform {
    backend "azurerm" {
      resource_group_name  = "terraform"
      storage_account_name = "wladiohstateterraform"
      container_name       = "tfstate"
      key                  = "codelab.microsoft.tfstate"
      // access_key           = "${var.backend_accesskey}"
    }
}

# Create a resource group
resource "azurerm_resource_group" "k8s" {
  name     = "${var.prefix}-k8s"
  location = "${var.location}"
}

resource "azurerm_kubernetes_cluster" "k8s" {
  name                = "${var.prefix}-k8s"
  location            = "${azurerm_resource_group.k8s.location}"
  resource_group_name = "${azurerm_resource_group.k8s.name}"
  dns_prefix          = "${var.prefix}-k8s"

  agent_pool_profile {
    name                      = "default"
    count                     = 1
    vm_size                   = "Standard_B2s"
    os_type                   = "Linux"
    os_disk_size_gb           = 30
    type                      = "VirtualMachineScaleSets"
    enable-cluster-autoscaler = true
    min_count                 = 1
    max_count                 = 2
  }

  service_principal {
    client_id     = "${var.client_id}"
    client_secret = "${var.client_secret}"
  }
  
  tags = {
    Environment = "develop"
  }  
}

resource "local_file" "kubeconfig" {
  content  = "${azurerm_kubernetes_cluster.k8s.kube_config_raw}"
  filename = "./${var.prefix}-k8s"
  depends_on = [azurerm_resource_group.k8s]
}

provider "helm" {
    kubernetes {
        config_path = "${local_file.kubeconfig.filename}"
    }
}

resource "helm_release" "nginx-ingress" {
    name      = "nginx-ingress"
    chart     = "stable/nginx-ingress"
    namespace =  "ingress-nginx"
}

resource "null_resource" "install-linkerd" {
  provisioner "local-exec" {
    command = "linkerd install --kubeconfig=${local_file.kubeconfig.filename} | kubectl --kubeconfig=${local_file.kubeconfig.filename} apply -f -"
  }
  
  provisioner "local-exec" {
    command = "kubectl apply -f ./linkerd-ingress.yaml --kubeconfig=${local_file.kubeconfig.filename}"
  }

  // provisioner "local-exec" {
  //   when = "destroy"
  //   command = "kubectl delete -f ./linkerd-ingress.yaml --kubeconfig=${local_file.kubeconfig.filename}"
  // }

  // provisioner "local-exec" {
  //   when = "destroy"
  //   command = "linkerd install --ignore-cluster --kubeconfig=${local_file.kubeconfig.filename} | kubectl --kubeconfig=${local_file.kubeconfig.filename} delete -f -"
  // }
  depends_on = [azurerm_resource_group.k8s, local_file.kubeconfig]
}

resource "null_resource" "logging" {
  provisioner "local-exec" {
    working_dir = "../"
    command = "kubectl create namespace kube-logging --kubeconfig=./terraform/${local_file.kubeconfig.filename}"
  }
  
  provisioner "local-exec" {
    working_dir = "../"
    command = "kubectl apply -n kube-logging -f elasticsearch.yaml -f filebeat.yaml -f logstash.yaml -f curator-cronjob.yaml -f kibana.yaml  --kubeconfig=./terraform/${local_file.kubeconfig.filename}"
  }

  provisioner "local-exec" {
    working_dir = "../"
    when = "destroy"
    command = "kubectl delete namespace kube-logging --kubeconfig=./terraform/${local_file.kubeconfig.filename}"
  }

  depends_on = [azurerm_resource_group.k8s, local_file.kubeconfig]
}