# terraform init -backend-config="storage_account_name=wladiohstateterraform" -backend-config="container_name=tfstate" -backend-config="access_key=" -backend-config="key=codelab.microsoft.tfstate"
provider "azurerm"  {
  # whilst the `version` attribute is optional, we recommend pinning to a given version of the Provider
  version = "1.33.1"
  subscription_id = "cf979063-433f-4e43-9a61-df40231fcd7e"
  client_id       = "${var.client_id}"
  client_secret   = "${var.client_secret}" 
  tenant_id       = "1bf562e0-3df8-4dd4-867b-11e99fa72ad4"
}

terraform {
    backend "azurerm" {}
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
    name            = "default"
    count           = 1
    vm_size         = "Standard_B2s"
    os_type         = "Linux"
    os_disk_size_gb = 30
  }

  service_principal {
    client_id     = "${var.client_id}"
    client_secret = "${var.client_secret}"
  }
  
  // provisioner "local-exec" {
  //   command =  |
  //     curl -sL https://run.linkerd.io/install | sh
  //     export PATH=$PATH:$HOME/.linkerd2/bin
  //     linkerd version
  //     linkerd check --pre
  //     linkerd install | kubectl apply -f -   
  //     linkerd check
  // }

  tags = {
    Environment = "developement"
  }  
}

resource "local_file" "kubeconfig" {
  content  = "${azurerm_kubernetes_cluster.k8s.kube_config_raw}"
  filename = "./${var.prefix}-k8s"
  depends_on = [azurerm_resource_group.k8s]
}

resource "null_resource" "apply_config_map_auth" {
  provisioner "local-exec" {
    command = "kubectl apply -f https://raw.githubusercontent.com/kubernetes/dashboard/v2.0.0-beta1/aio/deploy/recommended.yaml --kubeconfig=${local_file.kubeconfig.filename} >> private_ips.txt"
  }

  // provisioner "local-exec" {
  //   when = "destroy"
  //   command = "kubectl delete -f https://raw.githubusercontent.com/kubernetes/dashboard/v2.0.0-beta1/aio/deploy/recommended.yaml --kubeconfig=${local_file.kubeconfig.filename}"
  // }

  depends_on = [local_file.kubeconfig]
}

// resource "null_resource" "install-linkerd" {
//   provisioner "local-exec" {
//     command = "linkerd install | kubectl --kubeconfig=${local_file.kubeconfig.filename} apply -f -"
//   }

//   provisioner "local-exec" {
//     when = "destroy"
//     command = "linkerd install --ignore-cluster | kubectl --kubeconfig=${local_file.kubeconfig.filename} delete -f -"
//   }
//   depends_on = [local_file.kubeconfig]
// }

provider "helm" {
    kubernetes {
        config_path = "${local_file.kubeconfig.filename}"
    }
}

resource "helm_release" "mydatabase" {
    name      = "nginx-ingress"
    chart     = "stable/nginx-ingress"
    namespace =  "ingress-nginx"
}
