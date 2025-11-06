LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE := SerialPlugin
LOCAL_SRC_FILES := SerialPlugin.cpp

LOCAL_LDLIBS := -llog

include $(BUILD_SHARED_LIBRARY)