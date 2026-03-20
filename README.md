# WindowsAudioController

> WindowsのオーディオミキサーをTCP経由でリモートコントロールするC#アプリケーション

## 概要

Windowsのオーディオデバイスおよびアプリごとのミキサー音量を、ネットワーク経由（TCP）で操作できるサーバーアプリケーションです。
接続用のQRコードを自動生成・画面表示し、スマートフォン等のクライアントから接続して音量を制御することを想定して作成しました。

## 技術スタック

- 言語: C#（.NET Framework）
- UI: Windows Forms
- 音声制御: NAudio（CoreAudioApi）
- JSON: Newtonsoft.Json
- QRコード: ZXing.Net
- 通信: TCP（System.Net.Sockets）
- IDE: Visual Studio（.slnファイルあり）
- 対象OS: Windows

## 機能

- 起動時にローカルIPアドレスとポート番号をQRコードで画面右下に表示
- TCPサーバーとして待機し、クライアントの接続を受け付ける
- `GET_VOLUME` コマンドで現在のオーディオデバイス名・マスター音量・アプリごとの音量をJSON形式で返信
- `デバイス名/アプリ名,音量値` 形式のコマンドでマスター音量またはアプリ単位の音量を変更
- NAudio CoreAudioApi を通じてWindows標準のオーディオミキサーを直接制御
- クライアント切断後に自動で再接続待機状態に戻る

## 使い方 / 動かし方

Visual Studioでソリューションファイル（`WindowsVolumeController.sln`）を開き、ビルド・実行してください。

```
WindowsVolumeController.sln をVisual Studioで開く
→ ビルド → 実行
```

1. アプリを起動するとQRコードが画面右下に表示される
2. スマートフォンなどでQRコードを読み取り、表示されたIPアドレス・ポート番号にTCP接続する
3. `GET_VOLUME` を送信して音量情報を取得する
4. `デバイス名,0.5` などを送信してマスター音量を変更する（0.0〜1.0の範囲）

## 状態

基本動作は実装済み。ただし接続先IPアドレスが `192.168.0.*` のネットワーク固定のため、環境によっては修正が必要。機能追加・リファクタリングは停止中。
