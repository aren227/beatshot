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
            if (shape.ignoreRecorder) continue;

            ShapeSnapshot shapeSnapshot = new ShapeSnapshot();

            shapeSnapshot.color = shape.spriteRenderer.color;
            shapeSnapshot.type = shape.props.type;
            shapeSnapshot.position = shape.spriteRenderer.transform.position;
            shapeSnapshot.rotation = shape.transform.eulerAngles.z;
            shapeSnapshot.scale = shape.spriteRenderer.transform.lossyScale;
            shapeSnapshot.order = shape.spriteRenderer.sortingOrder;

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
        foreach (ShapeSnapshot shapeSnapshot in shapeSnapshots) {
            Shape shape = Shape.Create(shapeSnapshot.type);

            shape.transform.position = shapeSnapshot.position;

            Vector3 eulerAngles = shape.transform.eulerAngles;
            eulerAngles.z = shapeSnapshot.rotation;
            shape.transform.eulerAngles = eulerAngles;

            shape.transform.localScale = shapeSnapshot.scale;
            shape.SetColor(shapeSnapshot.color);

            shape.spriteRenderer.sortingOrder = shapeSnapshot.order;

            shape.DoNextFrame(0);
        }
    }
}

public class ShapeSnapshot {
    public ShapeType type;
    public Color color;
    public Vector3 position;
    public float rotation;
    public Vector3 scale;
    public int order;
}