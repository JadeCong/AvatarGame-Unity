BVHエクスポータ説明書

・概要
キャラクターの動きからアニメーションデータ(.bvh)を出力します。

・インストール
Ｃ＃のスクリプトBVHBase.cs,BVHExAll.cs,BVHExNormal.cs,BVHExMecanim.csをアセットとして追加してください。
UNITYバージョン３以下では、Mecanimを使用できないので、BVHExMecanim.csは除いてください。

・使い方
全部のノードを純粋に出力したい場合は、BVHExAll.csをルートにドラッグ＆ドロップしてください。
（これを使用した場合、指などの細かい動きも出力できます）
Mecanimが設定されたキャラクターを使う場合は、BVHExMecanim.csをキャラクターのルートにドラック＆ドロップして設定してください。
Mecanimが設定されてないキャラクターを使う場合は、BVHExNormal.csをキャラクターのルートにドラック＆ドロップして設定してください。
両方とも大まかな使い方は同じですが、BVHExNormalの場合は各ノードを手動で設定する必要があります。
Folder Nameの項目で設定されたフォルダに、Take～.bvhというファイル名で自動的に保存されていきます。

・各パラメータの説明
-Folder Name
  フォルダ名を指定します。（デフォルトでは、ドキュメントフォルダが指定されます）
-Right Hand Coordinate
  右手座標系として出力する場合は、チェックをいれます。（チェックがなければ左手座標系）
-Original Name
  UNITYで指定されている本来のノード名で出力します。
  チェックを外すと、決まった名前で出力します。(Hips,Chest,RightHandといったような)
-FromOrigin
  原点を中心として記録したい場合は、チェックしてください。
  チェックしない場合は、キャプチャー開始した地点が中心となります。
-Scale
  大きさにスケールをかけたい場合は、倍率を指定します。
-Capture Mode
  デフォルトのFromStartToFinishを選ぶと実行中の最初から最後まで記録します。
  ShortcutKeyを選ぶと、ショートカットキーで記録と記録終了ができます。
  RecordButtonを選ぶと、キャプチャー用のボタンが表示されます。
-Shortcut Key
  Capture ModeがShortcutKeyの場合、どのキーでキャプチャー開始、終了するのか選べます。
-各種ノード(Hips,Chest,Right Handなど。BVHExNormalのみ)
  それぞれに見合ったノードを設定してください。
  Hipsは、必ず設定する必要があります。

・注意点
実行開始する際に、キャラクターのポーズはＴポーズのようなニュートラルな状態である必要があります。
実行中でのパラメータ変更はしないでください。
Mecanimを使ったスクリプトでは、一部のノード設定が無い場合にエラーが表示される場合がありますが、正しくノード設定してあればデータ出力に問題はありません。


*チュートリアルビデオ
<a href="http://www.youtube.com/watch?v=D3bWK1hrKa4">http://www.youtube.com/watch?v=D3bWK1hrKa4</a>
(字幕　日本語/英語)<br><br>
