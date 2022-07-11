terraform {
  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "= 2.17.0"
    }

    kubernetes = {
      source = "hashicorp/kubernetes"
      version = "= 2.12.1"
    }
  }
}
