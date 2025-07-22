try {
	// 初始化文件处理器
	foreach (var line in FileProcessor.InitializeProcessor()) {
		Console.WriteLine(line);
	}
	Console.WriteLine($"CubeMX Version: {FileProcessor.CubeMXVersion}");
} catch (FileNotFoundException e) {
	Console.Error.WriteLine($"File not found when initializing processor: {e.Message}");
	return 1;
} catch (InvalidOperationException e) {
	Console.Error.WriteLine($"Invalid operation be caught when initializing processor: {e.Message}");
	return 2;
} catch (Exception e) {
	Console.Error.WriteLine($"Unexpected error occurred when initializing processor: {e.Message}");
	return -1;
}

const string linePrefix = "${CMAKE_CURRENT_SOURCE_DIR}/../../";
var tempFileInfo = FileProcessor.CubeMXCMakeListsTempFileInfo;
var originalFileInfo = FileProcessor.CubeMXCMakeListsFileInfo;

try {
	// 处理 CubeMX CMakeLists.txt 文件
	using (var writer = tempFileInfo.CreateText())
	using (var reader = originalFileInfo.OpenText()) {
		string? line = null;

		while ((line = reader.ReadLine()) != null) {
			var updated = false;

			var trimmedLine = line.TrimStart();
			var emptyPrefix = line.AsSpan(0, line.Length - trimmedLine.Length);
			trimmedLine = trimmedLine.TrimEnd();

			if (trimmedLine.StartsWith(linePrefix) && trimmedLine.EndsWith(".c")) {
				var relativePath = trimmedLine[linePrefix.Length..];
				if (FileProcessor.TryUpdateToCPPFile(ref relativePath, out var warning)) {
					// 如果更新成功，写入新的行
					writer.WriteLine(string.Concat(emptyPrefix, linePrefix, relativePath));
					updated = true;

					if (warning is not null) {
						Console.WriteLine(warning);
					}

					Console.WriteLine($"Updated C file to CPP file: {relativePath}");
				}
			}

			if (!updated) {
				// 如果没有更新，直接写入原行
				writer.WriteLine(line);
			}
		}
	}

	// 使用临时文件替换原始文件
	_ = tempFileInfo.CopyTo(originalFileInfo.FullName, true); // 这里只能使用 CopyTo 方法，原始文件被 CubeMX 占用无法删除（包括 MoveTo 方法）
	Console.WriteLine($"Successfully updated {originalFileInfo.Name}.");

} catch (Exception e) {
	Console.Error.WriteLine($"An error occurred during file processing: {e.Message}");

	return 3; // 返回一个错误码
} finally {
	// 尝试删除临时文件
	try {
		tempFileInfo.Refresh();
		if (tempFileInfo.Exists) {
			tempFileInfo.Delete();
		}
	} catch (Exception deleteEx) {
		Console.Error.WriteLine($"Failed to delete temporary file {tempFileInfo.Name}, please delete it manually: {deleteEx.Message}");
	}
}

return 0;