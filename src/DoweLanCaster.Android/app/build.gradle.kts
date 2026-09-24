plugins {
    id("com.android.application")
}

android {
    namespace = "com.mrjohndowe.dowelancaster.companion"
    compileSdk = 37

    defaultConfig {
        applicationId = "com.mrjohndowe.dowelancaster.companion"
        minSdk = 26
        targetSdk = 37
        versionCode = 63
        versionName = "0.9.5.63"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
}
