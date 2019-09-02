# --- logging ---
kubectl create namespace kube-logging
kubectl apply -f elasticsearch.yaml -n kube-logging
kubectl apply -f filebeat.yaml -n kube-logging
kubectl apply -f logstash.yaml -n kube-logging
kubectl apply -f curator-cronjob.yaml -n kube-logging
kubectl apply -f kibana.yaml -n kube-logging
# --- metrics ---
kubectl apply -f prometheus-overview-dashboard-configmap.yaml -n kube-logging
helm install stable/prometheus --namespace kube-logging --name prometheus
kubectl apply -f grafana_config.yaml -n kube-logging
helm install stable/grafana -f grafana_values.yaml --namespace kube-logging --name grafana
kubectl apply -f metricbeat.yaml -n kube-logging
kubectl apply -f prometheus.yaml -n kube-logging
