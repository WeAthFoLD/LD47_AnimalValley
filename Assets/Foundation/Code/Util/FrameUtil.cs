using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FrameUtil {
    const int FrameRate = 60;

    public static float Frame2Time(int frame) {
        return ((float) frame) / FrameRate;
    }

    public static int Time2Frame(float time) {
        return (int) (time * FrameRate);
    }

}