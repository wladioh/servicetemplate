variable "prefix" {
  description = "A prefix used for all resources in this example"
  type = string
}

variable "location" {
  description = "The Azure Region in which all resources in this example should be provisioned"
  type = string
  default = "West US"
}

variable "client_id" {
  description = "The Client ID for the Service Principal to use for this Managed Kubernetes Cluster"
  type = string  
  default = "eda0f77e-f01b-400c-8280-1754278dfc67"
}

variable "client_secret" {
  description = "The Client Secret for the Service Principal to use for this Managed Kubernetes Cluster"
  type = string
}