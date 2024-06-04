using Godot;
using System;

public partial class ElasticOut : ElasticInterpolation
{

    private float _value;
    private float _power;
    private int _bounces;
    private float _scale;

  	public ElasticOut() {
        this._value = 0;
        this._power = 0;
        this._bounces = 0;
        this._scale = 0;
    }

		public ElasticOut(float value, float power, int bounces, float scale) {
        this.Init(value, power, bounces, scale);
    }

		public void Init(float value, float power, int bounces, float scale) {
        this._value = value;
        this._power = power;
        this._bounces = (int)(bounces * Mathf.Pi * (bounces % 2 == 0? 1f : -1));;
        this._scale = scale;
    }


    protected override float ApplyInternal(float a) {
        if (a <= 0f) {
            return 0f;
        }
        a = 1f - a;
        return 1f - Mathf.Pow(_value, _power * (a - 1f)) * Mathf.Sin(a * _bounces) * _scale;
    }
}
