using Godot;
using System;
using System.ComponentModel;

public class ElasticIn : ElasticInterpolation
{
    private float _value;
    private float _power;
    private int _bounces;
    private float _scale;

    public ElasticIn() {
        this._value = 0;
        this._power = 0;
        this._bounces = 0;
        this._scale = 0;
    }

    public ElasticIn(float value, float power, int bounces, float scale) {
        this.Init(value, power, bounces, scale);
    }

    public void Init(float value, float power, int bounces, float scale) {
        this._value = value;
        this._power = power;
        this._bounces = (int)(bounces * Mathf.Pi * (bounces % 2 == 0? 1f : -1));
        this._scale = scale;
    }

    protected override float ApplyInternal(float a) {
        if (a > 0.99f) {
            return 1f;
        }
        return Mathf.Pow(_value, _power * (a - 1)) * Mathf.Sin(a * _bounces) * _scale;
    }
}
