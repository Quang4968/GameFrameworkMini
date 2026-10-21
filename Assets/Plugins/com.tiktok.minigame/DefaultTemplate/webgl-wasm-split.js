module.exports = {
	originalWasmMd5: "$ORIGINALWASMMD5",
    wasmTableSize: "$WASMTABLESIZE",
    wasmSplitVersion: "$WASMSPLITVERSION",
    subJsUrl: "$SUBJSURL",
    usingWasmH5: "$USINGWASMH5", 
    enableWasmCollect: "$ENABLEWASMCOLLECT",
    enableWasmSplit: "$ENABLEWASMSPLIT",
    globalVarList: ["$GLOBALVARLIST"],
    wasmCollectAPIVer: "v4",

    ANDROID_CODE_FILE_MD5: "$ANDROID_CODE_FILE_MD5",
    ANDROID_SUB_CODE_FILE_MD5: "$SUB_CODE_FILE_MD5", 
    ARCHIVE_CODE_FILE_MD5: "$ARCHIVE_CODE_FILE_MD5", 
    IOS_CODE_FILE_MD5: "$IOS_CODE_FILE_MD5",
    enableArchiveMode: "$ENABLEARCHIVEMODE",
    // Compatibility default. The local split CLI rewrites this literal to
    // true only after a successful archive split with a non-empty boot closure;
    // it permits an earlier parallel fetch, while main still waits for archive.
    archiveBootClosureReady: false
}
