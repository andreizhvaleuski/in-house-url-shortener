output "ihus_image_id" {
  depends_on = [
    docker_image.ihus
  ]
  description = "ID of the ihus docker image"
  value = docker_image.ihus.id
}
