   M S B A M L   `   `   `  ���� ?  ;View, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullclr-namespace:ViewView      View.MainWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     $localclr-namespace:View     -     � Title$  AgCubio��$	��400��$	��600�� � WindowStartupLocation$ CenterScreen=�.���  � ColumnDefinitions �� $��* ��� $	��200 �.+�'� -   $��viewport��$��0�� '� Camera =� -   
cameraMain�� � Position$ 35,15,701� � LookDirection$ -1,-.3,-1.555� � UpDirection$ 0,0,15� � FarPlaneDistance$ 30000Q� � NearPlaneDistance$ 1Q�	 =� FieldOfView$	 45Q�.x�g� 
 g� Content
 �� -   lightAmbient�� �� Color$ LightYellow��g� 
 t� -   lightKey��$ 	Goldenrod�� t� 	Direction$ -10,-25,-1005�g� 
 t� -   	lightSide��$ White��$ 1,2,05�g� 
 "� -   
lightPoint��$ 
LightGreen�� !� Position$ 	50,70,3001�g� -   modelBackground��
 �  � Geometry k�  k� 	Positions� �        L�@     L�@ 
� 
�     L�@ 
� 
� 
� 
�     L�@ 
� 
�     L�@     L�@ e� 
�     L�@ e� 
� 
� e�     L�@ 
� e� k� TriangleIndices. �$        � BackMaterial u�  u� Brush ��  �� Color$ Black��g� -	   
modelFloor��2�g� 
 �  k� U �        ���     ��@     ��@     ��@     ��@     ���     ���     ��� �      � Material u�  �� $ White�� �� Opacity$	 0.1Q�3  -System.Windows.Media.Media3D.ModelUIElement3D  -
   modelPlayCursor��   System.Windows.UIElement3D   IsHitTestVisible .     Model �  k� E �   ����@KL  � @KL @KL  � @KL ���� � �������� �  �      u�  �� $
 Pink�� "� 	Transform Y� .��� -   rotatorPointer��S� -   translatorPointer�� S� OffsetX$ 0Q� S� OffsetY$ 0Q� S� OffsetZ$ 0Q�g� -   groupPlayers3D��g� -   groupFood3D��]� -   $��1��
�������.+��� $	��Top=�.+��� -   $��lblPlayerName��$��WillFillInPlayerName��$��Center=�$��14(�$��Center=���  �� Orientation$ 
Horizontal=�.+��� $��Mass:���� -   $��lblMass��$��12345���� $ 
Horizontal=�.+��� $��Food:���� -   $��lblFoodCount��$��12345���� $ 
Horizontal=�.+��� $��Players:���� -   $��lblPlayerCount��$��12345���� $ 
Horizontal=�.+��� $��	Position:���� -   $��lblPlayerPosition��$
��null���� -   $��bttnDisconnect��$��
Disconnect��$��Bottom=�$	��100��$��Right=�$��3q��� -   $��txtbxMessages��$��	10,3,10,3q�$��Bottom=� �� TextWrapping$
 Wrap=� �� AcceptsReturn .  �� 
IsReadOnly . 