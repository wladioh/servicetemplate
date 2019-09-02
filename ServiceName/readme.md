# Startup Guide
## Helper Links

https://morelinq.github.io/
https://github.com/Humanizr/Humanizer
https://localhost/mini-profiler-resources/results

## Docker Compose
- The project have support to docker compose and run this project with docker compose run de follown command: 
```shell
docker-compose up
```
## Kubernets
- Dashboard
    https://kubernetes.io/docs/tasks/access-application-cluster/web-ui-dashboard/
    kubectl apply -f https://raw.githubusercontent.com/kubernetes/dashboard/v2.0.0-beta1/aio/deploy/recommended.yaml
    https://github.com/kubernetes/dashboard/wiki/Creating-sample-user
    kubectl -n kube-system describe secret $(kubectl -n kube-system get secret | grep admin-user | awk '{print $1}')
    eyJhbGciOiJSUzI1NiIsImtpZCI6IiJ9.eyJpc3MiOiJrdWJlcm5ldGVzL3NlcnZpY2VhY2NvdW50Iiwia3ViZXJuZXRlcy5pby9zZXJ2aWNlYWNjb3VudC9uYW1lc3BhY2UiOiJrdWJlLXN5c3RlbSIsImt1YmVybmV0ZXMuaW8vc2VydmljZWFjY291bnQvc2VjcmV0Lm5hbWUiOiJ0dGwtY29udHJvbGxlci10b2tlbi1ybjVmNCIsImt1YmVybmV0ZXMuaW8vc2VydmljZWFjY291bnQvc2VydmljZS1hY2NvdW50Lm5hbWUiOiJ0dGwtY29udHJvbGxlciIsImt1YmVybmV0ZXMuaW8vc2VydmljZWFjY291bnQvc2VydmljZS1hY2NvdW50LnVpZCI6ImMzM2E2ZmEzLWNiZmYtMTFlOS05YmE4LTAwMTU1ZDFkNWYyYiIsInN1YiI6InN5c3RlbTpzZXJ2aWNlYWNjb3VudDprdWJlLXN5c3RlbTp0dGwtY29udHJvbGxlciJ9.LkP0Bung_cyoaJiONXfN5lYMeexgL0hkoeNmC_VRJ26f5e-ZO26d25se4dm6hXkg0Bjx1jO6VXNtu0mkxa3a44dDTw2D88i6h1a7Q1aFXVD-2kYGKqjZI2zyRk0tNnAsqJjD1mvRCPV3iC9-vvWIrG-KliJElWX8hANiQIHQ-0j6BdVWrh_YU4AFduJZoLL66wuppK2LXmRx7ZGTYxJrlzzTkjG3FYFuw1mRR_Nkgbi9IbQUiIDQS_mr0ld5RQpu63b0-uq3kyYfB5RcXSxblqHAtqtIPI71QAvlC6jV1h76JC5h9mbgmfiZCxnoykE1dp5i2vs5xSIxQ8HBBjY61w
https://kubernetes.github.io/ingress-nginx/deploy/#provider-specific-steps
- install nginx ingress
    helm upgrade -i nginx-ingress stable/nginx-ingress --namespace ingress-nginx 
- Install K8s Ingress
    kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/master/deploy/static/mandatory.yaml
    kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/master/deploy/static/provider/cloud-generic.yaml
- Consul  https://learn.hashicorp.com/consul/getting-started-k8s/helm-deploy
https://www.consul.io/docs/platform/k8s/helm.html
https://learn.hashicorp.com/consul/day-1-operations/kubernetes-deployment-guide
    helm install ./ -f ../Tef/ServiceTemplate/ServiceName/deploy/Infra/helm-consul-values.yaml --name consul
    kubectl port-forward service/consul-consul-server 5000:8500

values.ya
- Create dev Namespace
    kubectl create namespace dev
- Grafana Prometheus
    https://medium.com/@chris_linguine/how-to-monitor-your-kubernetes-cluster-with-prometheus-and-grafana-2d5704187fc8
    Dashboards 
    10000
    8685
## Coverage
- The bootstrapper is used to download Cake and the tools required by the build script. This is (kind of) an optional step, but recommended since it removes the need to store binaries in the source code repository. 
- Sometimes PowerShell prevents you from running build.ps1. Here are some common scenarios and what to do about it.
- Effectively this is "RemoteSigned". Depending on how you start PowerShell, you could have different settings. You can read how to change your settings here http://go.microsoft.com/fwlink/?LinkID=135170. Assuming the MachinePolicy and UserPolicy settings are Undefined, you can running this relatively safe command to get RemoteSigned security:
```powershell
Set-ExecutionPolicy RemoteSigned -Scope Process
```
- Open up a Powershell prompt and execute the bootstrapper script:
```powershell
.\build.ps1
```
## Message bus
- todo
## Configuration Service
- todo
## Database
- todo
## Resiliencie with Refit and Polly
- This project use [Refit](https://github.com/reactiveui/refit) and [Polly](https://github.com/App-vNext/Polly)  libraries to make resilientes requests following [this](https://github.com/App-vNext/Polly/wiki/Transient-fault-handling-and-proactive-resilience-engineering) best praticies.
- Available Configuration with default values:
```json
{
   "Resiliencie": {
        "timeout": 5000,
        "cache":{
            "timeSpan": 5
        },
        "retry": {
            "maxRetries": 2,
            "maxDelay": 200
        },
        "circuitBreak": {
            "failureThreshold": 0.1,
            "samplingDuration": 3,
            "minimumThroughput": 10,
            "durationOfBreak": 5
        },
        "bulkhead":{
            "maxQueuingActions": 100,
            "maxParallelization": 100
        }
    }
}
```
### Explanation:
- Policies sequence execution
![Wrap policies](https://user-images.githubusercontent.com/9608723/32406632-bb7d2a06-c173-11e7-8a83-3f07549eb819.PNG "teste")

#### Timeout:
>Builds an timeout policy that will wait asynchronously for a delegate to complete for a specified period of time. A timeout exception will be thrown if the delegate does not complete within the configured timeout.
##### Parameters:
> - timeout: The timeout.
#### Cache:
> Builds an cache policy that will function like a result cache for delegate executions. Before executing a delegate, checks whether the cacheProvider holds a value for the cache key determined by applying the cacheKeyStrategy to the execution context. If the cacheProvider provides a value from cache, returns that value and does not execute the governed delegate. If the cacheProvider does not provide a value, executes the governed delegate, stores the value with the cacheProvider, then returns the value.
##### Parameters:
> - timeSpan: The timespan for which this cache-item remains valid.
---
#### Retry
> Builds an retry policy that will wait and retry as many times as there are provided sleep durations calling onRetryAsync on each retry withthe handled exception or result, the current sleep duration, retry count, andcontext data. On each retry, the duration to wait is the current sleep durations item.
##### Parameters:
> - maxRetries: The retry count.
> - maxDelay: The max duration to wait for a particular retry attempt.
---
#### Circuit Break:
>The circuit will break if, within any timeslice of duration samplingDuration, the proportion of actions resulting in a handled exception exceeds failureThreshold, provided also that the number of actions through the circuit in the timeslice is at least minimumThroughput. The circuit will stay broken for the durationOfBreak. Any attempt to execute this policy while the circuit is broken, will immediately throw a Exception containing the exception or result that broke the circuit. If the first action after the break duration period results in a handled exception or result, the circuit will break again for another durationOfBreak; if no exception or handled result is encountered, the circuit will reset.

##### Parameters:
> -  failureThreshold: 
        The failure threshold at which the circuit will break (a number between 0 and 1; eg 0.5 represents breaking if 50% or more of actions result in a handled failure.

> - samplingDuration:
        The duration of the timeslice over which failure ratios are assessed.

> - minimumThroughput:
        The minimum throughput: this many actions or more must pass through the circuit in the timeslice, for statistics to be considered significant and the circuit-breaker to come into action.
> - durationOfBreak:
    The duration the circuit will stay open before resetting.

#### Bulkhead:
Builds a bulkhead isolation policy, which limits the maximum concurrency of actions executed through the policy. Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system. When an execution would cause the number of actions executing concurrently through the policy to exceed maxParallelization, the policy allows a further maxQueuingActions executions to queue, waiting for a concurrent execution slot. When an execution would cause the number of queuing actions to exceed maxQueuingActions, a Bulkhead Rejected Exception is thrown.
##### Parameters:
> - maxParallelization: The maximum number of concurrent actions that may be executing through the policy.
> - maxQueuingActions: The maxmimum number of actions that may be queuing, waiting for an execution slot.