variable "REGISTRY" {
  default = ""
}

variable "TAG" {
  default = "latest"
}

group "default" {
  targets = ["credittransfer-api", "credittransfer-wcf"]
}

target "credittransfer-api" {
  context = ".."
  dockerfile = "src/Services/ApiServices/CreditTransferApi/Dockerfile"
  tags = ["${REGISTRY}credittransfer-api:${TAG}"]
  platforms = ["linux/amd64"]
}

target "credittransfer-wcf" {
  context = ".."
  dockerfile = "src/Services/WebServices/CreditTransferService/Dockerfile"
  tags = ["${REGISTRY}credittransfer-wcf:${TAG}"]
  platforms = ["linux/amd64"]
}

target "all" {
  contexts = {
    credittransfer-api = "target:credittransfer-api"
    credittransfer-wcf = "target:credittransfer-wcf"
  }
} 