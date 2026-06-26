using System;
using System.Collections.Generic;
using UnityEngine;

// ==========================================
// KELAS STATE (Menampung Fungsi Enter, Update, FixedUpdate, Exit)
// ==========================================
public class State
{
    public Action OnEnter;
    public Action OnUpdate;
    public Action OnFixedUpdate;
    public Action OnExit;

    // Konstruktor ini yang dicari oleh Unity saat Anda mengetik: new State(...)
    public State(Action onEnter, Action onUpdate, Action onFixedUpdate, Action onExit)
    {
        OnEnter = onEnter;
        OnUpdate = onUpdate;
        OnFixedUpdate = onFixedUpdate;
        OnExit = onExit;
    }
}

// ==========================================
// KELAS FSM MANAGER (Pengatur Perpindahan State)
// ==========================================
public class simpleFSM<T> where T : Enum
{
    public T currentstate { get; private set; }
    public bool StateInitialized { get; private set; } = false;

    private Dictionary<T, State> _stateDictionary;
    private State _currentStateDef;

    // Fungsi untuk memasukkan Dictionary State dari script musuh
    public void Initialize(Dictionary<T, State> states)
    {
        _stateDictionary = states;
        StateInitialized = true;
    }

    // Fungsi untuk memindahkan State secara aman
    public void ChangeState(T newState)
    {
        if (!StateInitialized) return;

        // 1. Jalankan fungsi Exit dari state sebelumnya (jika ada)
        if (_currentStateDef != null && _currentStateDef.OnExit != null)
        {
            _currentStateDef.OnExit.Invoke();
        }

        currentstate = newState;

        // 2. Ganti ke state baru dan jalankan fungsi Enter-nya
        if (_stateDictionary.TryGetValue(currentstate, out _currentStateDef))
        {
            if (_currentStateDef.OnEnter != null)
            {
                _currentStateDef.OnEnter.Invoke();
            }
        }
        else
        {
            Debug.LogWarning($"State {newState} tidak ditemukan di dalam Dictionary FSM!");
        }
    }

    // Fungsi untuk menjalankan logika Update secara terus-menerus
    public void Update()
    {
        if (!StateInitialized || _currentStateDef == null) return;

        if (_currentStateDef.OnUpdate != null)
        {
            _currentStateDef.OnUpdate.Invoke();
        }
    }

    // Fungsi tambahan jika musuh menggunakan Rigidbody (Fisika)
    public void FixedUpdate()
    {
        if (!StateInitialized || _currentStateDef == null) return;

        if (_currentStateDef.OnFixedUpdate != null)
        {
            _currentStateDef.OnFixedUpdate.Invoke();
        }
    }
}