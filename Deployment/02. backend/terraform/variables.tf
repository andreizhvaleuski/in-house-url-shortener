variable "ihus_image_name" {
  type    = string
  default = "ihus"
}

variable "ihus_image_tagged_name" {
  type    = string
  default = "ihus:dev"
}

variable "kubernetes_host" {
  type = string
}

variable "kubernetes_client_certificate" {
  type = string
}

variable "kubernetes_client_key" {
  type = string
}

variable "kubernetes_cluster_ca_certificate" {
  type = string
}

variable "ihus_pg-master-connection-string" {
  type      = string
  sensitive = true
}

variable "ihus_pg-slave-1-connection-string" {
  type      = string
  sensitive = true
}

variable "ihus_pg-slave-2-connection-string" {
  type      = string
  sensitive = true
}

variable "ihus_host_name" {
  type    = string
  default = "dev.ihus.com"
}
