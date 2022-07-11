provider "docker" {
  host = "npipe:////.//pipe//docker_engine"
}

resource "docker_image" "ihus" {
  name = var.ihus_image_name

  build {
    path = "../../../IHUS"
    tag = [ var.ihus_image_tagged_name ]
  }
}
