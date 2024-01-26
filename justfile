project := "gce-metadata-dotnet-test"
registry := "europe-west4-docker.pkg.dev" / project / "registry"

[no-cd]
set-project:
    gcloud config set project "{{project}}"

docker-build:
    docker build -t {{registry}}/metadata-test-server:latest --target final .
    docker push {{registry}}/metadata-test-server:latest

deploy-only:
    gcloud run deploy metadata-test \
      --project="{{project}}" \
      --use-http2 \
      --allow-unauthenticated \
      --region europe-west4 \
      --image={{registry}}/metadata-test-server:latest
      

deploy: docker-build deploy-only
