# output:
RELEASE=elasticsearch
REPLICAS=1
MIN_REPLICAS=1
STORAGE_CLASS=my-local-storage-class
#helm ls --all ${RELEASE} && helm del --purge ${RELEASE}

# helm template stable/elasticsearch  \
#       --name ${RELEASE} \
#       --set client.replicas=${MIN_REPLICAS} \
#       --set master.replicas=${REPLICAS} \
#       --set master.persistence.storageClass=${STORAGE_CLASS} \
#       --set data.replicas=${MIN_REPLICAS} \
#       --set data.persistence.storageClass=${STORAGE_CLASS} \
#       --set master.podDisruptionBudget.minAvailable=${MIN_REPLICAS} \
#       --set cluster.env.MINIMUM_MASTER_NODES=${MIN_REPLICAS} \
#       --set cluster.env.RECOVER_AFTER_MASTER_NODES=${MIN_REPLICAS} \
#       --set cluster.env.EXPECTED_MASTER_NODES=${MIN_REPLICAS} \
#       --namespace elasticsearch 

helm install stable/elasticsearch \
      --name ${RELEASE} \
      --set client.replicas=${MIN_REPLICAS} \
      --set client.additionalJavaOpts="-XX:MaxRAM=256m -Xms256m -Xmx256m" \
      --set master.replicas=${REPLICAS} \
      --set master.persistence.storageClass=${STORAGE_CLASS} \
      --set master.additionalJavaOpts="-XX:MaxRAM=256m -Xms256m -Xmx256m" \
      --set data.replicas=${MIN_REPLICAS} \
      --set data.persistence.enabled=false \
      --set data.persistence.storageClass=${STORAGE_CLASS} \
      --set data.additionalJavaOpts="-XX:MaxRAM=256m -Xms256m -Xmx256m" \
      --set master.podDisruptionBudget.minAvailable=${MIN_REPLICAS} \
      --set master.persistence.enabled=false \
      --set cluster.env.MINIMUM_MASTER_NODES=${MIN_REPLICAS} \
      --set cluster.env.RECOVER_AFTER_MASTER_NODES=${MIN_REPLICAS} \
      --set cluster.env.EXPECTED_MASTER_NODES=${MIN_REPLICAS} \
      --set cluster.env.ES_JAVA_OPTS="-XX:MaxRAM=256m -Xms256m -Xmx256m" \
      --set cluster.additionalJavaOpts="-XX:MaxRAM=256m -Xms256m -Xmx256m" \
      --namespace elasticsearch