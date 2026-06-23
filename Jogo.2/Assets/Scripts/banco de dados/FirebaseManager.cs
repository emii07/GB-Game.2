using Firebase;
using Firebase.Database;
using UnityEngine;
using System;

[Serializable]
public class DadosSessao {
    public int pontuacao;
    public float oscilacaoMedia;
    public string data;

    public DadosSessao(int p, float o) {
        pontuacao = p;
        oscilacaoMedia = o;
        data = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    }
}

public class FirebaseManager : MonoBehaviour
{
    DatabaseReference reference;

    [Header("Dados do Teste")]
    public string idPaciente = "tulio_delmiro";
    public int pontosParaSalvar = 150;
    public float oscilacaoParaSalvar = 1.45f;

    void Start() {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) {
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase pronto para salvar dados!");
            }
        });
    }

    public void SalvarPartida() {
        if (reference == null) {
            Debug.LogError("Firebase ainda não inicializou!");
            return;
        }

        DadosSessao novaSessao = new DadosSessao(pontosParaSalvar, oscilacaoParaSalvar);
        string json = JsonUtility.ToJson(novaSessao);

        string key = reference.Child("jogadores").Child(idPaciente).Child("historico_sessoes").Push().Key;

        reference.Child("jogadores")
            .Child(idPaciente)
            .Child("historico_sessoes")
            .Child(key)
            .SetRawJsonValueAsync(json)
            .ContinueWith(task => {
                if (task.IsCompleted) {
                    Debug.Log("Sucesso! Dados guardados no Firebase.");
                }
            });
    }                                                                                                                                                                      
}