using Firebase;
using Firebase.Database;
using UnityEngine;
using System;

// organizão dos dados da partida
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

    void Start() {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) {
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase pronto para salvar dados!");
            }
        });
    }

    // função pra chamar no fim do jogo
    public void SalvarPartida(int pontos, float oscilacao) {
        if (reference == null) return;

        // Cria um objeto com os dados atuais
        DadosSessao novaSessao = new DadosSessao(pontos, oscilacao);

        // Transforma em JSON (formato que o Firebase entende)
        string json = JsonUtility.ToJson(novaSessao);

        // Cria um ID único para cada partida (baseado no tempo) para não apagar a anterior
        string key = reference.Child("sessoes").Push().Key;

        // Salva no banco de dados dentro da pasta "sessoes"
        reference.Child("sessoes").Child(key).SetRawJsonValueAsync(json).ContinueWith(task => {
            if (task.IsCompleted) {
                Debug.Log("Sessão salva com sucesso no Firebase!");
            }
        });
    }
}