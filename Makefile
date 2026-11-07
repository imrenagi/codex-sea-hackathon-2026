GCP_PROJECT ?= ujian-sim-514002
GCP_REGION ?= asia-southeast2
WEB_SERVICE ?= jalan-kita-web
ARTIFACT_REPOSITORY ?= web
UNITY_CLI ?= unity
UNITY_PROJECT_DIR ?= Unity
WEB_BUILD_DIR ?= $(UNITY_PROJECT_DIR)/Builds/WebGL

.PHONY: build/web deploy/web
build/web:
	@UNITY_CLI="$(UNITY_CLI)" UNITY_PROJECT_DIR="$(UNITY_PROJECT_DIR)" \
		WEB_BUILD_DIR="$(WEB_BUILD_DIR)" sh deploy/web/build.sh

deploy/web: build/web
	@GCP_PROJECT="$(GCP_PROJECT)" GCP_REGION="$(GCP_REGION)" \
		WEB_SERVICE="$(WEB_SERVICE)" ARTIFACT_REPOSITORY="$(ARTIFACT_REPOSITORY)" \
		WEB_BUILD_DIR="$(WEB_BUILD_DIR)" sh deploy/web/deploy.sh
