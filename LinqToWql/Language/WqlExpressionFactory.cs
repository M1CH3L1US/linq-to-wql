﻿using System.Linq.Expressions;
using LinqToWql.Language.Expressions;
using LinqToWql.Language.Statements;

namespace LinqToWql.Language;

public class WqlExpressionFactory {
  public WhereWqlExpression MakeWhereExpression(
    Expression source,
    LambdaExpression expression,
    ExpressionChainType chainType
  ) {
    var inner = GetInnerExpressionFromLambda(expression);
    return new WhereWqlExpression(source, inner, chainType);
  }

  public Expression MakeSelectExpression(Expression source, LambdaExpression lambda) {
    var selectProperty = GetInnerMemberAccessFromLambda(lambda);
    return new SelectWqlStatement(source, selectProperty);
  }

  private WqlExpression GetInnerExpressionFromLambda(LambdaExpression lambda) {
    if (lambda.Body is MethodCallExpression methodCall) {
      return GetInnerMethodCallFromLambda(lambda, methodCall);
    }

    if (lambda.Body is BinaryExpression binary) {
      return ConvertToBinaryWqlExpression(binary);
    }

    throw new NotSupportedException();
  }

  private WqlExpression ConvertToWqlExpression(Expression expression) {
    if (expression is MemberExpression memberExpression) {
      return new PropertyWqlExpression(memberExpression.Member.Name);
    }

    if (expression is ConstantExpression constant) {
      return new ConstantWqlExpression(constant.Value);
    }

    if (expression is BinaryExpression binary) {
      return ConvertToBinaryWqlExpression(binary);
    }

    throw new NotSupportedException();
  }

  private BinaryWqlExpression ConvertToBinaryWqlExpression(BinaryExpression binary) {
    var left = ConvertToWqlExpression(binary.Left);
    var right = ConvertToWqlExpression(binary.Right);
    var op = binary.NodeType;

    return new BinaryWqlExpression(left, op, right);
  }

  private List<PropertyWqlExpression> GetInnerMemberAccessFromLambda(LambdaExpression lambda) {
    if (lambda.Body is MemberExpression memberAccess) {
      var member = memberAccess.Member.Name;
      return new List<PropertyWqlExpression> {new(member)};
    }

    if (lambda.Body is NewExpression newExpression) {
      return newExpression.Arguments.Select(ConvertToWqlExpression).Cast<PropertyWqlExpression>().ToList();
    }

    throw new NotSupportedException("The select operation is not supported");
  }

  private WqlExpression GetInnerMethodCallFromLambda(LambdaExpression lambda, MethodCallExpression methodCall) {
    var method = methodCall.Method;

    if (method.DeclaringType != typeof(WqlResourcePropertyQueryExtensions)) {
      throw new NotSupportedException("Only methods on WqlResourceProperties are supported");
    }

    var property = (MemberExpression) methodCall.Arguments.First();
    var argument = (ConstantExpression) methodCall.Arguments.Skip(1).First();

    var propertyName = property.Member.Name;

    return method.Name switch {
      nameof(WqlResourcePropertyQueryExtensions.IsA) =>
        new IsAWqlExpression(propertyName, (string) argument.Value!),
      nameof(WqlResourcePropertyQueryExtensions.Like) =>
        new LikeWqlExpression(propertyName, (string) argument.Value!),
      _ => throw new NotImplementedException()
    };
  }
}