﻿using System;

namespace Dman.LSystem.SystemCompiler.Linker
{
    public enum LinkExceptionType
    {
        CYCLIC_DEPENDENCY,
        MISSING_EXPORT,
        MISSING_FILE,
        IMPORT_COLLISION,
        IMPORT_DISSONANCE,
        GLOBAL_VARIABLE_COLLISION,
        BASE_FILE_IS_LIBRARY,
        BAD_ORIGIN_FILE,
        BAD_GLOBAL_PARAMETER,
        INVALID_CUSTOM_SYMBOL_CONFIGURATION,
    }

    public class LinkException : Exception
    {
        public LinkExceptionType exceptionType;
        public override string StackTrace => fileStack.Length <= 0 ? base.StackTrace : fileStack.JoinText("\n");
        private string[] fileStack;

        public LinkException(LinkExceptionType type, string message, params string[] file) : base(message)
        {
            fileStack = file;
            exceptionType = type;
        }
    }
}
