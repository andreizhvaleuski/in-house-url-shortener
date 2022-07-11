provider "kubernetes" {
  host = var.kubernetes_host

  client_certificate     = base64decode(var.kubernetes_client_certificate)
  client_key             = base64decode(var.kubernetes_client_key)
  cluster_ca_certificate = base64decode(var.kubernetes_cluster_ca_certificate)
}

resource "kubernetes_secret_v1" "ihus" {
  metadata {
    name = "ihus-secrets"
  }

  type = "Opaque"

  data = {
    pg-master-connection-string  = base64decode(var.ihus_pg-master-connection-string)
    pg-slave-1-connection-string = base64decode(var.ihus_pg-slave-1-connection-string)
    pg-slave-2-connection-string = base64decode(var.ihus_pg-slave-2-connection-string)
  }
}

resource "kubernetes_deployment_v1" "ihus-api-master" {
  depends_on = [
    docker_image.ihus,
    kubernetes_secret_v1.ihus
  ]

  metadata {
    name = "in-house-url-shortener-rw"
  }

  spec {
    selector {
      match_labels = {
        app  = "in-house-url-shortener"
        mode = "read-write"
      }
    }
    min_ready_seconds = 15
    template {
      metadata {
        labels = {
          app  = "in-house-url-shortener"
          mode = "read-write"
        }
      }
      spec {
        container {
          name              = "in-house-url-shortener"
          image             = var.ihus_image_tagged_name
          image_pull_policy = "Never"

          port {
            container_port = 80
          }

          env {
            name = "ConnectionStrings__Default"
            value_from {
              secret_key_ref {
                name = "ihus-secrets"
                key  = "pg-master-connection-string"
              }
            }
          }
        }
      }
    }
  }
}

resource "kubernetes_service_v1" "ihus-api-master-service" {
  depends_on = [
    kubernetes_deployment_v1.ihus-api-master
  ]

  metadata {
    name = "in-house-url-shortener-rw"
  }

  spec {
    port {
      port        = 5000
      target_port = 80
      protocol    = "TCP"
      name        = "http"
    }

    selector = {
      app  = "in-house-url-shortener"
      mode = "read-write"
    }
  }
}

resource "kubernetes_deployment_v1" "ihus-api-slave-1" {
  depends_on = [
    docker_image.ihus,
    kubernetes_secret_v1.ihus
  ]

  metadata {
    name = "in-house-url-shortener-ro-1"
  }

  spec {
    selector {
      match_labels = {
        app      = "in-house-url-shortener"
        mode     = "read-only"
        instance = 1
      }
    }
    min_ready_seconds = 15
    template {
      metadata {
        labels = {
          app      = "in-house-url-shortener"
          mode     = "read-only"
          instance = 1
        }
      }
      spec {
        container {
          name              = "in-house-url-shortener"
          image             = var.ihus_image_tagged_name
          image_pull_policy = "Never"

          port {
            container_port = 80
          }

          env {
            name = "ConnectionStrings__Default"
            value_from {
              secret_key_ref {
                name = "ihus-secrets"
                key  = "pg-slave-1-connection-string"
              }
            }
          }
        }
      }
    }
  }
}

resource "kubernetes_deployment_v1" "ihus-api-slave-2" {
  depends_on = [
    docker_image.ihus,
    kubernetes_secret_v1.ihus
  ]

  metadata {
    name = "in-house-url-shortener-ro-2"
  }

  spec {
    selector {
      match_labels = {
        app      = "in-house-url-shortener"
        mode     = "read-only"
        instance = 2
      }
    }
    min_ready_seconds = 15
    template {
      metadata {
        labels = {
          app      = "in-house-url-shortener"
          mode     = "read-only"
          instance = 2
        }
      }
      spec {
        container {
          name              = "in-house-url-shortener"
          image             = var.ihus_image_tagged_name
          image_pull_policy = "Never"

          port {
            container_port = 80
          }

          env {
            name = "ConnectionStrings__Default"
            value_from {
              secret_key_ref {
                name = "ihus-secrets"
                key  = "pg-slave-2-connection-string"
              }
            }
          }
        }
      }
    }
  }
}

resource "kubernetes_service_v1" "ihus-api-slaves-service" {
  depends_on = [
    kubernetes_deployment_v1.ihus-api-slave-1,
    kubernetes_deployment_v1.ihus-api-slave-2
  ]

  metadata {
    name = "in-house-url-shortener-ro"
  }

  spec {
    port {
      port        = 5000
      target_port = 80
      protocol    = "TCP"
      name        = "http"
    }

    selector = {
      app  = "in-house-url-shortener"
      mode = "read-only"
    }
  }
}

resource "kubernetes_manifest" "ihus-virtual-server" {
  depends_on = [
    kubernetes_service_v1.ihus-api-master-service,
    kubernetes_service_v1.ihus-api-slaves-service
  ]

  manifest = {
    apiVersion = "k8s.nginx.org/v1"
    kind       = "VirtualServer"

    metadata = {
      name      = "ihus"
      namespace = "default"
    }

    spec = {
      host = var.ihus_host_name

      upstreams = [
        {
          name    = "ro"
          service = "in-house-url-shortener-ro"
          port    = "5000"
        },
        {
          name    = "rw"
          service = "in-house-url-shortener-rw"
          port    = "5000"
        }
      ]

      routes = [
        {
          path = "/api"

          matches = [
            {
              conditions = [
                {
                  variable = "$request_method"
                  value    = "GET"
                }
              ]

              action = {
                pass = "ro"
              }
            }
          ]

          action = {
            pass = "rw"
          }
        }
      ]
    }
  }
}
