project := `gcloud config get-value project`
registry := "europe-west4-docker.pkg.dev" / project / "registry"

[private]
default:
    @just --list

# set the gcloud project to the specified projectName
[no-cd]
set-project projectName: 
    gcloud config set project "{{projectName}}"

# list all the projects in the gcloud account
[no-cd]
list-projects:
    gcloud projects list

# Build the container and push it to the specified project registry
[confirm('Confirm you want to build and push to the specified project?')]
docker-build-push:
    docker build -t {{registry}}/metadata-test-server:latest --target final .
    docker push {{registry}}/metadata-test-server:latest

# deploy the container to the specified project via cloud run
[no-cd]
deploy-only:
    gcloud run deploy metadata-test \
      --project="{{project}}" \
      --use-http2 \
      --allow-unauthenticated \
      --region europe-west4 \
      --image={{registry}}/metadata-test-server:latest

[private]
echo-project:
    @echo "Container will be deployed to gcloud project {{project}}"
    @echo "Please make sure the following registry exists: {{registry}}"

# build & deploy the container to the specified project via cloud run
deploy: echo-project docker-build-push deploy-only
