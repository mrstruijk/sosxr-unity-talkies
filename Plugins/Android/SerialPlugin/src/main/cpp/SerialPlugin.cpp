#include <jni.h>
#include <string>
#include <android/log.h>

#define TAG "SerialPlugin"
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO, TAG, __VA_ARGS__)

extern "C" {

static JavaVM* g_vm = nullptr;

jclass serialBridgeClass = nullptr;
jmethodID methodOpen = nullptr;
jmethodID methodWrite = nullptr;
jmethodID methodRead = nullptr;
jmethodID methodClose = nullptr;

JNIEnv* GetEnv() {
    JNIEnv* env = nullptr;
    g_vm->AttachCurrentThread(&env, nullptr);
    return env;
}

JNIEXPORT jint JNICALL JNI_OnLoad(JavaVM* vm, void*) {
    g_vm = vm;
    JNIEnv* env = GetEnv();

    jclass clazz = env->FindClass("com/sosxr/serial/SerialBridge");
    serialBridgeClass = (jclass)env->NewGlobalRef(clazz);

    methodOpen  = env->GetStaticMethodID(serialBridgeClass, "open",  "(Landroid/app/Activity;I)Z");
    methodWrite = env->GetStaticMethodID(serialBridgeClass, "write", "([BI)I");
    methodRead  = env->GetStaticMethodID(serialBridgeClass, "read",  "([BI)I");
    methodClose = env->GetStaticMethodID(serialBridgeClass, "close", "()V");

    return JNI_VERSION_1_6;
}

JNIEXPORT jint JNICALL Java_com_unity3d_player_UnityPlayer_nativeRender() {
    return 0;
}

// -------- Your exported plugin API (Unity calls these) -------- //

JNIEXPORT jint JNICALL SerialOpen(const char* portName, jint baudRate) {
    JNIEnv* env = GetEnv();

    jclass unityPlayer = env->FindClass("com/unity3d/player/UnityPlayer");
    jfieldID activityField = env->GetStaticFieldID(unityPlayer, "currentActivity", "Landroid/app/Activity;");
    jobject activity = env->GetStaticObjectField(unityPlayer, activityField);

    jboolean result = env->CallStaticBooleanMethod(
        serialBridgeClass, methodOpen, activity, baudRate);

    return result ? 1 : 0;
}

JNIEXPORT jint JNICALL SerialWrite(jbyte* data, jint len) {
    JNIEnv* env = GetEnv();
    jbyteArray arr = env->NewByteArray(len);
    env->SetByteArrayRegion(arr, 0, len, data);

    jint written = env->CallStaticIntMethod(serialBridgeClass, methodWrite, arr, len);
    env->DeleteLocalRef(arr);

    return written;
}

JNIEXPORT jint JNICALL SerialRead(jbyte* buffer, jint bufferSize) {
    JNIEnv* env = GetEnv();
    jbyteArray arr = env->NewByteArray(bufferSize);

    jint n = env->CallStaticIntMethod(serialBridgeClass, methodRead, arr, bufferSize);

    if (n > 0) {
        env->GetByteArrayRegion(arr, 0, n, buffer);
    }

    env->DeleteLocalRef(arr);
    return n;
}

JNIEXPORT void JNICALL SerialClose() {
    JNIEnv* env = GetEnv();
    env->CallStaticVoidMethod(serialBridgeClass, methodClose);
}

} // extern "C"
