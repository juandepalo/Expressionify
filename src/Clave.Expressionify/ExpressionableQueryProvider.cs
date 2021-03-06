﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Clave.Expressionify
{
    public class ExpressionableQueryProvider : IAsyncQueryProvider
    {
        private readonly IQueryProvider _underlyingQueryProvider;

        public ExpressionableQueryProvider(IQueryProvider underlyingQueryProvider)
        {
            _underlyingQueryProvider = underlyingQueryProvider;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new ExpressionableQuery<TElement>(this, expression);
        }

        public IQueryable CreateQuery(Expression expression)
        {
            try
            {
                var elementType = expression.Type.GetElementType();
                var type = typeof(ExpressionableQuery<>).MakeGenericType(elementType);
                var args = new object[] { this, expression };
                return (IQueryable)Activator.CreateInstance(type, args);
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        internal IEnumerable<T> ExecuteQuery<T>(Expression expression)
        {
            return _underlyingQueryProvider.CreateQuery<T>(Visit(expression)).AsEnumerable();
        }

        internal IAsyncEnumerable<T> ExecuteQueryAsync<T>(Expression expression)
        {
            return _underlyingQueryProvider.CreateQuery<T>(Visit(expression)).AsAsyncEnumerable();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _underlyingQueryProvider.Execute<TResult>(Visit(expression));
        }

        public object Execute(Expression expression)
        {
            return _underlyingQueryProvider.Execute(Visit(expression));
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            if(_underlyingQueryProvider is IAsyncQueryProvider provider)
            {
#pragma warning disable EF1001 // Internal EF Core API usage.
                return provider.ExecuteAsync<TResult>(Visit(expression), cancellationToken);
#pragma warning restore EF1001 // Internal EF Core API usage.
            }

            throw new Exception("This shouldn't happen");
        }

        private Expression Visit(Expression exp)
        {
            return new ExpressionifyVisitor().Visit(exp);
        }
    }
}
