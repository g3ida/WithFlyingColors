using Godot;
using System;

public partial class PlayerRotationAction: GodotObject
{
    private const float DEFAULT_ROTATION_DURATION = 0.1f;
    private CountdownTimer rotationTimer = new CountdownTimer();
    
    private float duration;
    public float thetaZero = 0.0f; // initial angle, before the rotation is performed.
    private float thetaTarget = 0.0f; // target angle, after the rotation is completed.
    private float thetaPoint = 0.0f; // the calculated angle.
    public bool canRotate = true; // set to false when rotation is in progress.
    private CharacterBody2D body;


    public PlayerRotationAction()
    {
        // FIXME remove this constructor after c# migration
    }

    public void Set(CharacterBody2D _body)
    {
        rotationTimer.Set(DEFAULT_ROTATION_DURATION, false);
        body = _body;
    }

    public void Step(float delta)
    {
        if (rotationTimer.IsRunning())
        {
            rotationTimer.Step(delta);
            if (!rotationTimer.IsRunning())
            {
                // last frame correction
                float currentAngle = body.Rotation;
                thetaPoint = (thetaTarget - currentAngle) / delta;
                rotationTimer.Stop();
            }
            body.Rotate(thetaPoint * delta);
        }
        else if (!canRotate)
        {
            thetaPoint = 0.0f;
            rotationTimer.Stop();
            canRotate = true;
        }
    }

    // FIXME: remove commented optional params after c# migration
    public bool Execute(int direction, float angleRadians /*= Mathf.Pi * 2*/, float _duration /*= DEFAULT_ROTATION_DURATION*/, bool shouldForce /*= true*/, bool cumulateTarget /*= true*/, bool useRound /*= true*/)
    {
        if (!canRotate && !shouldForce) return false;
        canRotate = false;
        duration = _duration;
        rotationTimer.Set(duration, false);

        thetaZero = body.Rotation;

        if (Math.Abs(thetaPoint) > Mathf.Epsilon && cumulateTarget)
        {
            thetaZero = thetaTarget;
        }

        float unroundedAngle = Mathf.DegToRad(Mathf.RadToDeg(thetaZero + direction * angleRadians)) / angleRadians;
        if (useRound)
        {
            thetaTarget = Mathf.Round(unroundedAngle) * angleRadians;
        }
        else
        {
            float roundedAngle = direction == -1 ? Mathf.Ceil(unroundedAngle) : Mathf.Floor(unroundedAngle);
            thetaTarget = roundedAngle * angleRadians;
        }

        if (Math.Abs(thetaPoint) > Mathf.Epsilon && cumulateTarget)
        {
            thetaZero = body.Rotation;
        }

        thetaPoint = (thetaTarget - thetaZero) / duration;
        rotationTimer.Reset();
        return true;
    }
}
