using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeRecorder
{
    List<FrameSnapshot> frameSnapshots = new List<FrameSnapshot>();

    public float lastRecordTime;

    public void TakeSnapshot() {
        FrameSnapshot frameSnapshot = new FrameSnapshot();

        lastRecordTime = frameSnapshot.time = Manager.Instance.time;

        foreach (Shape shape in GameObject.FindObjectsOfType<Shape>()) {
            ShapeSnapshot shapeSnapshot = new ShapeSnapshot();

            shapeSnapshot.color = shape.spriteRenderer.color;
            shapeSnapshot.position = shape.spriteRenderer.transform.position;
            shapeSnapshot.scale = shape.spriteRenderer.transform.lossyScale;

            frameSnapshot.shapeSnapshots.Add(shapeSnapshot);
        }

        frameSnapshots.Add(frameSnapshot);
    }

    public void Show(float time) {
        FrameSnapshot minFrameShapshot = null;
        float minError = float.PositiveInfinity;

        foreach (FrameSnapshot snapshot in frameSnapshots) {
            float curError = Mathf.Abs(time - snapshot.time);
            if (curError < minError) {
                minError = curError;
                minFrameShapshot = snapshot;
            }
        }

        if (minFrameShapshot != null) {
            minFrameShapshot.Show();
        }
    }
}

public class FrameSnapshot {
    public float time;
    public List<ShapeSnapshot> shapeSnapshots = new List<ShapeSnapshot>();

    public void Show() {
        Debug.Log("Show frame at " + time);

        foreach (ShapeSnapshot shapeSnapshot in shapeSnapshots) {
            Shape shape = Shape.Create();

            shape.transform.position = shapeSnapshot.position;
            shape.transform.localScale = shapeSnapshot.scale;
            shape.SetColor(shapeSnapshot.color);

            shape.DoNextFrame(0);
        }
    }
}

public class ShapeSnapshot {
    public Color color;
    public Vector3 position;
    public Vector3 scale;
}