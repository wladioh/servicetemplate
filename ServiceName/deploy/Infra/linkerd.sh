linkerd install | kubectl apply -f -
linkerd check
kubectl apply -k github.com/weaveworks/flagger/kustomize/linkerd