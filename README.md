# Auditório Shunji Nishimura

Uma simulação interativa de auditório para palestras e eventos desenvolvida em **Unity** com suporte a multiplayer através da rede FishNet.

## 📋 Visão Geral

Este projeto implementa um auditório virtual completo do **Shunji Nishimura**, permitindo que múltiplos usuários se conectem simultaneamente para assistir palestras, eventos e apresentações em tempo real. A arquitetura utiliza a biblioteca de networking FishNet para sincronização de estados entre clientes.

## 🎮 Características Principais

### Funcionalidades do Auditório
- **Espaço Virtual Imersivo**: Representação fiel do auditório físico em 3D
- **Múltiplos Usuários**: Suporte para vários participantes simultâneos via rede
- **Sincronização em Tempo Real**: Posições, rotações e ações dos participantes sincronizadas através da rede
- **Controle de Personagem**: Sistema de movimento e interação para usuários
- **Ambientação Realista**: Modelos 3D de mobiliário, iluminação e acústica do auditório

### Tecnologias de Rede
- **FishNet Networking**: Framework multiplayer com serialização automática
- **LiteNetLib**: Transporte UDP de baixa latência
- **Client-Side Prediction**: Previsão do lado cliente para melhor responsividade
- **Network Synchronization**: Sincronização automática de estados entre clientes

## 📁 Estrutura do Projeto

```
Shunji-Nishimura/
├── Assets/
│   ├── FishNet/                    # Framework de networking
│   │   ├── CodeGenerating/        # Geração de código para serialização
│   │   └── Runtime/               # Runtime do networking
│   ├── Scripts/                    # Scripts do auditório
│   │   └── PlayerController.cs    # Sistema de controle do jogador
│   ├── Scenes/                     # Cenas do projeto
│   ├── Prefabs/                    # Prefabs reutilizáveis
│   ├── Materials/                  # Materiais visuais
│   ├── Sprites/                    # Sprites 2D
│   ├── Models/                     # Modelos 3D (mobiliário, estrutura)
│   │   ├── Brick Project Studio/  # Elementos de design
│   │   ├── Chair_armchair set2/   # Cadeiras do auditório
│   │   ├── Doors/                 # Portas
│   │   ├── Environment/           # Elementos do ambiente
│   │   ├── Fire_Extinguisher/     # Extintores
│   │   └── school/                # Objetos escolares
│   ├── TextMesh Pro/              # Sistema de texto 3D
│   └── Videos/                    # Vídeos para reprodução
├── Packages/                       # Dependências do Unity
├── Build/                          # Builds compilados
│   ├── Client/                     # Build do cliente
│   └── Server/                     # Build do servidor
├── Logs/                           # Logs de execução
└── ProjectSettings/                # Configurações do projeto Unity

```

## 🚀 Como Começar

### Pré-requisitos
- **Unity 2022 LTS** ou superior
- **C# 10.0+**
- Conhecimento básico de networking em Unity

### Instalação

1. **Clone o repositório**
   ```bash
   git clone https://github.com/BentoCarlos/Shunji-Nishimura.git
   cd Shunji-Nishimura
   ```

2. **Abra o projeto no Unity**
   - Abra o Unity Hub
   - Selecione "Add project"
   - Navegue até a pasta do projeto
   - Abra o projeto

3. **Aguarde a importação de assets**
   - O Unity importará automaticamente todos os modelos 3D e configurações

### Executando o Projeto

1. **Abra a cena principal**
   - Navegue para `Assets/Scenes/`
   - Abra a cena principal do auditório

2. **Configure o servidor (opcionalmente)**
   - Execute a build do servidor em `Build/Server/`
   - Ou use um servidor de desenvolvimento local

3. **Execute o cliente**
   - Clique em "Play" no Unity Editor
   - Ou execute a build do cliente em `Build/Client/`

## 🎮 Controles do Jogador

O **PlayerController** oferece os seguintes controles:

| Ação | Controle |
|------|----------|
| Movimento | WASD ou Analógico |
| Câmera | Mouse / Analógico Direito |
| Interação | E |
| Sprint | Shift |
| Pular | Espaço |

## 🌐 Arquitetura de Networking

### Sistema de Sincronização

O projeto utiliza **FishNet** para sincronização automática:

- **NetworkBehaviour**: Componentes sincronizados via rede
- **Reader/Writer**: Serialização automática de dados
- **Replicates**: Estados sincronizados periodicamente
- **Reconciliation**: Correção de previsão do lado cliente

## 📦 Dependências Principais

### FishNet Networking
- **Versão**: Com suporte a Yak 1.0.0
- **Localização**: `Assets/FishNet/`
- **Uso**: Sincronização multiplayer automática

### GameKit Utilities
- Funções auxiliares para matemática e utilitários
- Extensões para tipos do Unity

### LiteNetLib
- Transporte UDP de baixa latência
- Configuração automática via FishNet

### TextMesh Pro
- Sistema de texto 3D renderizado
- UI avançada no mundo 3D

## 🔧 Estrutura de Scripts Principais

### PlayerController.cs
Sistema de controle do jogador com suporte a networking:
- Movimento e rotação
- Sincronização de posição/rotação via rede
- Previsão do lado cliente
- Interações com o ambiente

### NetworkBehaviours
Componentes sincronizados pela rede para:
- Posição e rotação de personagens
- Estados de interação
- Animações
- Áudio sincronizado

## 🎨 Customização

### Adicionar Novos Elementos ao Auditório

1. **Modelos 3D**: Coloque os arquivos em `Assets/Prefabs/` ou `Assets/Models/`
2. **Materiais**: Configure em `Assets/Materials/`
3. **Scripts**: Herde de `NetworkBehaviour` para sincronização automática
4. **Cena**: Adicione à cena principal

### Modificar Controles do Jogador

Edite `Assets/Scripts/PlayerController.cs` para ajustar:
- Velocidade de movimento
- Sensibilidade da câmera
- Ações e interações

## 📊 Compilação e Build

O projeto conta com múltiplos projetos C#:

```
Assembly-CSharp.csproj              # Código principal do jogo
Assembly-CSharp-Editor.csproj       # Ferramentas do editor
FishNet.Runtime.csproj              # Runtime de networking
FishNet.Codegen.Cecil.csproj        # Geração de código
FishNet.Demos.csproj                # Demos do FishNet
GameKit.Utilities.csproj            # Utilitários
```

### Para Compilar uma Build

1. Vá para `File > Build Settings`
2. Selecione a plataforma desejada (PC, Android, WebGL)
3. Clique em "Build" ou "Build and Run"

## 📚 Referências e Documentação

- **FishNet Docs**: https://fish-networking.gitbook.io/docs/
- **Unity Networking**: https://docs.unity3d.com/Manual/NetworkingOverview.html
- **LiteNetLib**: https://github.com/RevenantX/LiteNetLib

## 👥 Contribuintes

- **Desenvolvedor Principal**: BentoCarlos
- **Projeto de Auditório**: Shunji Nishimura