using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TableGen : Editor
{
	[MenuItem("Assets/刷新配置数据")]
	static void GenData()
	{
		var batFilePath = "Assets\\..\\Table\\gen.bat";
		var worlFilePath = "Assets\\..\\Table";
		if (ExecuteProcess(batFilePath, worlFilePath))
		{
			Debug.Log($"表格数据已更新");
		}
	}

	static bool ExecuteProcess(string batFilePath, string workingDirectory)
	{
		var dirInfo = new DirectoryInfo(batFilePath);
		if (!File.Exists(dirInfo.FullName))
		{
			Debug.LogError($"not find path:{dirInfo.FullName}");
			return false;
		}
		var workDirInfo = new DirectoryInfo(workingDirectory);
		if (!Directory.Exists(workDirInfo.FullName))
		{
			Debug.LogError($"not find path:{workDirInfo.FullName}");
			return false;
		}

		ProcessStartInfo start = new ProcessStartInfo();
		start.FileName = dirInfo.FullName;
		start.WorkingDirectory = workDirInfo.FullName;
		start.UseShellExecute = true;
		start.CreateNoWindow = true;
		var process = Process.Start(start);
		process.WaitForExit();
		process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
		{
			Debug.LogError(e.Data);
		};
		return process.ExitCode == 0;
	}
}
