using System;
using System.Text;
using SQLite;
using System.Collections.Generic;

#if !PORTABLE
using System.Runtime.CompilerServices;
#endif

#if PORTABLE
using Infragistics.Portable.Runtime;
#endif

#if PCL
namespace Infragistics.Core.Controls.DataSource
#else
namespace Infragistics.Controls.DataSource
#endif
{
	/// <summary>
	/// Emits OData literals.
	/// </summary>
    public interface ISQLiteLiteralEmitter
    {
		/// <summary>
		/// Emits a literal value.
		/// </summary>
		/// <param name="value">The value to emit.</param>
		/// <param name="leaveUnquoted">Wheter to leave a string literal unquoted.</param>
		/// <returns></returns>
        string EmitLiteral(object value, bool leaveUnquoted);
    }

	/// <summary>
	/// A default implementation of an SQLite literal emitter.
	/// </summary>
    public class DefaultSQLiteLiteralEmitter
        : ISQLiteLiteralEmitter
    {
		/// <summary>
		/// Emits an odata formatted literal.
		/// </summary>
		/// <param name="value">The value to emit.</param>
		/// <param name="leaveUnquoted">Whether the value should be left unquoted if a string.</param>
		/// <returns>The literal string.</returns>
        public string EmitLiteral(object value, bool leaveUnquoted)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }
            else if (value is DateTime)
            {
                return EmitDateTime((DateTime)value);
            }
            else if (value is TimeSpan)
            {
                return EmitTimespan((TimeSpan)value);
            }
            else if (value is DateTimeOffset)
            {
                return EmitDateTimeOffset((DateTimeOffset)value);
            }
            //else if (value is int)
            //{
            //    return EmitInteger((int)value);
            //}
            //else if (value is long)
            //{
            //    return EmitLong((long)value);
            //}
            //else if (value is short)
            //{
            //    return EmitShort((short)value);
            //}
            else if (value is string)
            {
                var ret = value.ToString();
                if (!leaveUnquoted)
                {
                    ret = "'" + ret + "'";
                }
                return ret;
            }
            else
            {
                //TODO: need more processing here?
                return value.ToString();
            }
        }

        private string EmitTimespan(TimeSpan value)
        {
            return value.Ticks.ToString();
        }

		private string EmitDateTimeOffset(DateTimeOffset value)
        {
            value = value.ToUniversalTime();
            return string.Format("'{0}", value.ToString("yyyy-MM-dd HH:mm:ss.fffffffzzz") + "'");
        }

		private string EmitDateTime(DateTime value)
        {
            if (value.Kind == DateTimeKind.Local)
            {
                value = value.ToUniversalTime();
            }

            string ret;
            if (value.Second == 0 && value.Millisecond == 0)
            {
                ret = value.ToString("yyyy-MM-dd HH:mm");
            }
            else if (value.Millisecond == 0)
            {
                ret = value.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                ret = value.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
            }

            return "'" + ret + "'";
        }
    }

	/// <summary>
	/// Visits an a filter expression and emits an odata expression.
	/// </summary>
    public class SQLiteDataSourceFilterExpressionVisitor
        : FilterExpressionVisitor
    {
        private StringBuilder _sb;
        private ISQLiteLiteralEmitter _literalEmitter;
        private TableMapping _mappings;

        /// <summary>
        /// Constructs an SQLiteDataSourceFilterExpressionVisitor.
        /// </summary>
        public SQLiteDataSourceFilterExpressionVisitor(TableMapping mappings)
        {
            _mappings = mappings;
            _literalEmitter = new DefaultSQLiteLiteralEmitter();
            _sb = new StringBuilder();
        }

		/// <summary>
		/// Constructs an SQLiteDataSourceFilterExpressionVisitor providing an alternative literal emitter.
		/// </summary>
		/// <param name="literalEmitter">An alternative literal emitter to use.</param>
        public SQLiteDataSourceFilterExpressionVisitor(TableMapping mappings, ISQLiteLiteralEmitter literalEmitter)
            : this(mappings)
        {
            _literalEmitter = literalEmitter;
        }

		/// <summary>
		/// Gets the resulting string to use.
		/// </summary>
		/// <returns>The string to use.</returns>
        public override string ToString()
        {
            return _sb.ToString();
        }

		/// <summary>
		/// Visits an operation expression.
		/// </summary>
		/// <param name="expression">The operation expression to visit.</param>
        public override void VisitOperationExpression(OperationFilterExpression expression)
        {
            bool isBinary = true;
            string operatorString = "";
            switch (expression.Operator)
            {
                case FilterExpressionOperatorType.Add:
                    operatorString = "+";
                    break;
                case FilterExpressionOperatorType.And:
                    operatorString = "AND";
                    break;
                case FilterExpressionOperatorType.Divide:
                    operatorString = "/";
                    break;
                case FilterExpressionOperatorType.None:
                case FilterExpressionOperatorType.Equal:
                    operatorString = "==";
                    break;
                case FilterExpressionOperatorType.GreaterThan:
                    operatorString = ">";
                    break;
                case FilterExpressionOperatorType.GreaterThanOrEqual:
                    operatorString = ">=";
                    break;
                case FilterExpressionOperatorType.Grouping:
                    isBinary = false;
                    break;
                case FilterExpressionOperatorType.LessThan:
                    operatorString = "<";
                    break;
                case FilterExpressionOperatorType.LessThanOrEqual:
                    operatorString = "<=";
                    break;
                case FilterExpressionOperatorType.Modulo:
                    operatorString = "%";
                    break;
                case FilterExpressionOperatorType.Multiply:
                    operatorString = "*";
                    break;
                case FilterExpressionOperatorType.Not:
                    operatorString = "NOT";
                    isBinary = false;
                    break;
                case FilterExpressionOperatorType.NotEqual:
                    operatorString = "!=";
                    break;
                case FilterExpressionOperatorType.Or:
                    operatorString = "OR";
                    break;
                case FilterExpressionOperatorType.Subtract:
                    operatorString = "-";
                    break;
                default:
                    operatorString = "==";
                    break;
            }

            if (isBinary)
            {
                Visit(expression.Left);
                _sb.Append(" ");
                _sb.Append(operatorString);
                _sb.Append(" ");
                Visit(expression.Right);
            }
            else
            {
                if (expression.Operator == FilterExpressionOperatorType.Grouping)
                {
                    _sb.Append("(");
                }
                else
                {
                    _sb.AppendLine(operatorString + " ");
                }

                if (expression.Left != null)
                {
                    Visit(expression.Left);
                }
                else
                {
                    Visit(expression.Right);
                }

                if (expression.Operator == FilterExpressionOperatorType.Grouping)
                {
                    _sb.Append(")");
                }
            }
        }

		/// <summary>
		/// Visits a function expression.
		/// </summary>
		/// <param name="expression">The function expression to visit.</param>
        public override void VisitFunctionExpression(FunctionFilterExpression expression)
        {
            string functionName = null;
            string prefix = null;
            string between = null;
            string postfix = null;
            bool unquote = false;

            switch (expression.FunctionType)
            {
                case FilterExpressionFunctionType.Ceiling:
                    throw new NotImplementedException();
                    //functionName = "ceiling";
                    break;
                case FilterExpressionFunctionType.Concat:
                    between = " || ";
                    break;
                case FilterExpressionFunctionType.Contains:
                    unquote = true;
                    between = " LIKE '%";
                    postfix = "%'";
                    break;
				case FilterExpressionFunctionType.Day:
                    throw new NotImplementedException();
                    break;
				case FilterExpressionFunctionType.EndsWith:
                    unquote = true;
                    between = " LIKE '%";
                    postfix = "'";
                    break;
                case FilterExpressionFunctionType.Floor:
                    throw new NotImplementedException();
                    break;
                case FilterExpressionFunctionType.Hour:
                    throw new NotImplementedException();
                    break;
                case FilterExpressionFunctionType.IndexOf:
                    functionName = "intstr";
                    break;
                case FilterExpressionFunctionType.Length:
                    functionName = "length";
                    break;
                case FilterExpressionFunctionType.Minute:
                    throw new NotImplementedException();
                    break;
                case FilterExpressionFunctionType.Month:
                    throw new NotImplementedException();
                    break;
                case FilterExpressionFunctionType.Replace:
                    functionName = "replace";
                    break;
                case FilterExpressionFunctionType.Round:
                    functionName = "round";
                    break;
                case FilterExpressionFunctionType.Second:
                    throw new NotImplementedException();
                    break;
                case FilterExpressionFunctionType.StartsWith:
                    unquote = true;
                    between = " LIKE '";
                    postfix = "%'";
                    break;
                case FilterExpressionFunctionType.Substring:
                    functionName = "substr";
                    break;
                case FilterExpressionFunctionType.ToLower:
                    functionName = "lower";
                    break;
                case FilterExpressionFunctionType.ToUpper:
                    functionName = "upper";
                    break;
                case FilterExpressionFunctionType.Trim:
                    functionName = "trim";
                    break;
                case FilterExpressionFunctionType.Year:
                    throw new NotImplementedException();
                    break;
            }

            if (functionName != null)
            {
                _sb.Append(functionName);
                _sb.Append("(");
            }
            if (prefix != null)
            {
                _sb.Append(prefix);
            }
            bool first = true;

            for (var i = 0; i < expression.FunctionArguments.Count; i++)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    if (between != null)
                    {
                        _sb.Append(between);
                    }
                    else
                    {
                        _sb.Append(", ");
                    }
                }

                SuppressQuotes(true);
                Visit(expression.FunctionArguments[i]);
                UnsuppressQuotes();
            }

            if (functionName != null)
            {
                _sb.Append(")");
            }
            if (postfix != null)
            {
                _sb.Append(postfix);
            }
        }

        private Stack<bool> _quotesSuppressed = new Stack<bool>();
        private void UnsuppressQuotes()
        {
            _quotesSuppressed.Pop();
        }

        private void SuppressQuotes(bool suppress)
        {
            _quotesSuppressed.Push(suppress);
        }

        private bool AreQuotesSuppressed()
        {
            if (_quotesSuppressed.Count == 0 || !_quotesSuppressed.Peek())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Visits a literal expression.
        /// </summary>
        /// <param name="expression">The literal expression to visit.</param>
        public override void VisitLiteralExpression(LiteralFilterExpression expression)
        {
            RenderLiteral(expression, expression.LiteralValue);
        }

        private void RenderLiteral(LiteralFilterExpression expression, object literalValue)
        {
            var value = _literalEmitter.EmitLiteral(literalValue, expression.LeaveUnquoted);

            if (AreQuotesSuppressed() && value is string)
            {
                if (value.StartsWith("'") && value.EndsWith("'"))
                {
                    value = value.Substring(1, value.Length - 2);
                }
            }
            if (AreQuotesSuppressed() && value is string)
            {
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2);
                }
            }
            //TODO: need to do special stuff here for dates etc.

            // JM 02-01-2016 - Graham: Please review this
            //if (literalValue is DateTime || literalValue is DateTimeOffset)
            //	value = "datetime'" + value + "'";

            _sb.Append(value);
        }

		/// <summary>
		/// Visits a property reference expression.
		/// </summary>
		/// <param name="expression">The expression to visit.</param>
        public override void VisitPropertyReferenceExpression(PropertyReferenceFilterExpression expression)
        {
            //This is working around a missing property on the filter api.
            //This could can be removed when the issue is resolved.
            ODataDataSourceFilterExpressionVisitor v = new ODataDataSourceFilterExpressionVisitor();
            v.VisitPropertyReferenceExpression(expression);
            string result = v.ToString();

            RenderPropertyReference(result);
        }

        private void RenderPropertyReference(string propertyReference)
        {
            var col = _mappings.FindColumnWithPropertyName(propertyReference);
            if (col != null)
            {
                _sb.Append(col.Name);
            }
            else
            {
                _sb.Append(propertyReference);
            }
        }
    }
     

}